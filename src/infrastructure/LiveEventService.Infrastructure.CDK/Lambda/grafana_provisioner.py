import boto3
import json
import os
import urllib.request
import urllib.error
import ssl


def send_cfn_response(event, context, status, data=None, reason=None):
    response_url = event.get('ResponseURL')
    if not response_url:
        return
    response_body = {
        'Status': status,
        'Reason': reason or f'See the details in CloudWatch Log Stream: {context.log_stream_name}',
        'PhysicalResourceId': context.log_stream_name,
        'StackId': event['StackId'],
        'RequestId': event['RequestId'],
        'LogicalResourceId': event['LogicalResourceId'],
        'Data': data or {}
    }
    data = json.dumps(response_body).encode('utf-8')
    req = urllib.request.Request(response_url, data=data, method='PUT')
    req.add_header('content-type', '')
    req.add_header('content-length', str(len(data)))
    ctx = ssl.create_default_context()
    with urllib.request.urlopen(req, context=ctx) as resp:
        resp.read()


def get_dashboard_json(s3, s3_uri):
    # s3_uri format: s3://bucket/key
    if not s3_uri or not s3_uri.startswith('s3://'):
        return None
    _, _, rest = s3_uri.partition('s3://')
    bucket, _, key = rest.partition('/')
    obj = s3.get_object(Bucket=bucket, Key=key)
    return obj['Body'].read().decode('utf-8')


def ensure_prom_datasource(grafana_url, api_key, region, amp_workspace_id):
    # Create or update Prometheus data source with uid 'prom'
    payload = {
        "name": "Prometheus",
        "type": "prometheus",
        "uid": "prom",
        "access": "proxy",
        "url": f"https://aps-workspaces.{region}.amazonaws.com/workspaces/{amp_workspace_id}/api/v1/query",
        "jsonData": {
            "httpMethod": "POST",
            "sigV4Auth": True,
            "sigV4Region": region
        }
    }
    data = json.dumps(payload).encode('utf-8')
    headers = {
        'Authorization': f'Bearer {api_key}',
        'Content-Type': 'application/json'
    }
    # Try update by UID first
    try:
        req = urllib.request.Request(f"{grafana_url}/api/datasources/uid/prom", data=data, method='PUT', headers=headers)
        with urllib.request.urlopen(req) as resp:
            resp.read()
            return
    except urllib.error.HTTPError as e:
        if e.code not in (404, 400):
            raise
    # Create
    req = urllib.request.Request(f"{grafana_url}/api/datasources", data=data, method='POST', headers=headers)
    with urllib.request.urlopen(req) as resp:
        resp.read()


def import_dashboard(grafana_url, api_key, dashboard_json):
    payload = {
        'dashboard': json.loads(dashboard_json),
        'overwrite': True,
        'folderId': 0
    }
    data = json.dumps(payload).encode('utf-8')
    headers = {
        'Authorization': f'Bearer {api_key}',
        'Content-Type': 'application/json'
    }
    req = urllib.request.Request(f"{grafana_url}/api/dashboards/db", data=data, method='POST', headers=headers)
    with urllib.request.urlopen(req) as resp:
        resp.read()


def handler(event, context):
    request_type = event.get('RequestType', 'Create')
    try:
        if request_type in ('Create', 'Update'):
            region = os.environ['AWS_REGION']
            workspace_id = os.environ['GRAFANA_WORKSPACE_ID']
            endpoint = os.environ['GRAFANA_ENDPOINT']  # e.g., https://g-xyz.grafana-workspace.eu-west-1.amazonaws.com
            amp_workspace_id = os.environ['AMP_WORKSPACE_ID']
            s3_uris = os.environ.get('DASHBOARD_URIS', '')

            grafana = boto3.client('grafana', region_name=region)
            s3 = boto3.client('s3', region_name=region)

            # Create short-lived admin API key
            api_key_resp = grafana.create_workspace_api_key(
                keyName='cdk-provision',
                keyRole='ADMIN',
                secondsToLive=3600,
                workspaceId=workspace_id
            )
            api_key = api_key_resp['key']

            # Ensure Prometheus datasource exists (uid 'prom')
            ensure_prom_datasource(endpoint, api_key, region, amp_workspace_id)

            # Import dashboards
            for uri in filter(None, [u.strip() for u in s3_uris.split(',')]):
                body = get_dashboard_json(s3, uri)
                if body:
                    import_dashboard(endpoint, api_key, body)

            send_cfn_response(event, context, 'SUCCESS', data={'Status': 'Provisioned'})
        else:
            # Delete: nothing to clean up
            send_cfn_response(event, context, 'SUCCESS', data={'Status': 'Deleted'})
    except Exception as e:
        send_cfn_response(event, context, 'FAILED', reason=str(e))


