BEGIN TRANSACTION;
DROP TABLE IF EXISTS "Sites";
CREATE TABLE IF NOT EXISTS "Sites" (
	"Name"	TEXT NOT NULL,
	"State"	INTEGER NOT NULL,
	"XML"	TEXT NOT NULL,
	"LastUpdated"	DateTime NOT NULL,
	"Language"	TEXT NOT NULL,
	"Description"	TEXT NOT NULL,
	"IsAdult"	INTEGER NOT NULL,
	"DllId"	TEXT,
	"OwnerId"	text NOT NULL,
	PRIMARY KEY("Name"),
	FOREIGN KEY("OwnerId") REFERENCES "Users"("Email"),
	FOREIGN KEY("DllId") REFERENCES "Dlls"("Name")
);
DROP TABLE IF EXISTS "Reports";
CREATE TABLE IF NOT EXISTS "Reports" (
	"Id"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	"Date"	DateTime NOT NULL,
	"Message"	TEXT NOT NULL,
	"Type"	INTEGER NOT NULL,
	"SiteName"	TEXT NOT NULL,
	FOREIGN KEY("SiteName") REFERENCES "Sites"("Name")
);
DROP TABLE IF EXISTS "Dlls";
CREATE TABLE IF NOT EXISTS "Dlls" (
	"Name"	TEXT NOT NULL,
	"LastUpdated"	DateTime NOT NULL,
	"MD5"	TEXT NOT NULL,
	"OwnerId"	text NOT NULL,
	PRIMARY KEY("Name"),
	FOREIGN KEY("OwnerId") REFERENCES "Users"("Email")
);
DROP TABLE IF EXISTS "Users";
CREATE TABLE IF NOT EXISTS "Users" (
	"Email"	TEXT,
	"Password"	TEXT,
	"IsAdmin"	INTEGER,
	PRIMARY KEY("Email")
);
DROP INDEX IF EXISTS "Reports_SiteName";
CREATE INDEX IF NOT EXISTS "Reports_SiteName" ON "Reports" (
	"SiteName"	ASC
);
COMMIT;
