-- =============================================================
-- Scavengy – Table Definitions
-- 
-- PURPOSE:  Documentation only. Describes the intended schema
--           in standard SQL Server syntax.
-- RUNTIME:  The application uses SQLite via EF Core, which
--           manages the actual schema through EnsureCreated().
-- =============================================================

BEGIN TRANSACTION T;

CREATE TABLE Hunts (
    Id              INT             NOT NULL PRIMARY KEY IDENTITY(1,1),
    HuntLocation    NVARCHAR(200)   NOT NULL DEFAULT '',
    Title           NVARCHAR(300)   NOT NULL DEFAULT '',
    CreatedDate     DATETIME2       NOT NULL
);

CREATE TABLE Clues (
    Id              INT             NOT NULL PRIMARY KEY IDENTITY(1,1),
    HuntId          INT             NOT NULL,
    ClueIndex       INT             NOT NULL,
    ClueText        NVARCHAR(MAX)   NOT NULL DEFAULT '',
    LocationName    NVARCHAR(MAX)   NOT NULL DEFAULT '',
    LocationAddress NVARCHAR(MAX)   NULL,
    Latitude        DECIMAL(9, 6)   NULL,
    Longitude       DECIMAL(9, 6)   NULL,

    CONSTRAINT FK_Clues_Hunts FOREIGN KEY (HuntId) REFERENCES Hunts (Id) ON DELETE CASCADE
);

CREATE INDEX IX_Clues_HuntId_ClueIndex ON Clues (HuntId, ClueIndex);

COMMIT TRANSACTION T;