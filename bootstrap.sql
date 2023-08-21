CREATE TABLE "Pessoas" (
    "Id" uuid NOT NULL,
    "Apelido" text NULL,
    "Nome" text NULL,
    "Nascimento" date NOT NULL,
    "Stack" text[] NULL,
    "StackSearch" text NULL,
    CONSTRAINT "PK_Pessoas" PRIMARY KEY ("Id"),
    CONSTRAINT "UNQ_Pessoas_Apelido" UNIQUE ("Apelido")
);
CREATE INDEX "IX_Pessoas_StackSearch" ON "Pessoas" USING GIN (to_tsvector('english', "StackSearch" || ' ' || "Apelido" || ' ' || "Nome"));