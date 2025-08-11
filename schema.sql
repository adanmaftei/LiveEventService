CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Events" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" character varying(4000) NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Capacity" integer NOT NULL DEFAULT 100,
    "TimeZone" character varying(100) NOT NULL,
    "Location" character varying(500) NOT NULL,
    "OrganizerId" character varying(450) NOT NULL,
    "IsPublished" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Events" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "IdentityId" character varying(450) NOT NULL,
    "Email" character varying(256) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "PhoneNumber" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "EventRegistrations" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RegistrationDate" timestamp with time zone NOT NULL,
    "Status" text NOT NULL,
    "Notes" character varying(1000),
    "PositionInQueue" integer,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_EventRegistrations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventRegistrations_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventRegistrations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_EventRegistrations_EventId_UserId" ON "EventRegistrations" ("EventId", "UserId");

CREATE INDEX "IX_EventRegistrations_RegistrationDate" ON "EventRegistrations" ("RegistrationDate");

CREATE INDEX "IX_EventRegistrations_Status" ON "EventRegistrations" ("Status");

CREATE INDEX "IX_EventRegistrations_UserId" ON "EventRegistrations" ("UserId");

CREATE INDEX "IX_Events_IsPublished" ON "Events" ("IsPublished");

CREATE INDEX "IX_Events_OrganizerId" ON "Events" ("OrganizerId");

CREATE INDEX "IX_Events_StartDate" ON "Events" ("StartDate");

CREATE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE UNIQUE INDEX "IX_Users_IdentityId" ON "Users" ("IdentityId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250724175956_InitialCreate', '9.0.7');

COMMIT;

