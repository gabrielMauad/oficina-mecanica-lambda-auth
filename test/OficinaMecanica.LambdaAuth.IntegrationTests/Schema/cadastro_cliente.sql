-- Copia manual do trecho relevante da migration EF Core da aplicacao (fonte de verdade):
--   oficina-mecanica-v2/src/Modules/Cadastro/Cadastro.Infrastructure/Persistence/Migrations/
--   20260503211924_InitialCreate.cs (tabela "cliente", schema "cadastro")
--
-- Ponto de deriva conhecido: se a aplicacao alterar essa tabela (renomear coluna, mudar tipo,
-- constraint), esta copia so muda quando alguem lembrar de atualiza-la manualmente. Ver README
-- ("Deriva de schema conhecida") para a mitigacao sugerida (rodar as migrations reais no CI).

CREATE SCHEMA IF NOT EXISTS cadastro;

CREATE TABLE cadastro.cliente (
    id uuid NOT NULL,
    nome character varying(200) NOT NULL,
    documento character varying(14) NOT NULL,
    email character varying(200) NOT NULL,
    telefone character varying(20) NOT NULL,
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_cliente" PRIMARY KEY (id),
    CONSTRAINT "CK_cliente_documento_digits" CHECK (documento ~ '^[0-9]+$')
);

CREATE UNIQUE INDEX "IX_cliente_documento" ON cadastro.cliente (documento);
