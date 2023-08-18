CREATE TABLE "Pessoas" (
    "Id" uuid NOT NULL,
    "Apelido" text NULL,
    "Nome" text NULL,
    "Nascimento" date NOT NULL,
    "Stack" text[] NULL,
    "SearchString" text NULL,
    CONSTRAINT "PK_Pessoas" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_Pessoas_SearchString" ON "Pessoas" ("SearchString" text_pattern_ops);