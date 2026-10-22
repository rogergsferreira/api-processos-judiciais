# ⚖️ API de Gestão de Processos Judiciais

[![CI](https://github.com/rogergsferreira/api-processos-judiciais/actions/workflows/ci.yml/badge.svg)](https://github.com/rogergsferreira/api-processos-judiciais/actions/workflows/ci.yml)
[![Licença: MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-green.svg)](LICENSE)

![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=.net&logoColor=white)

API REST em .NET para o gerenciamento de processos judiciais e do histórico de movimentações, com testes de integração automatizados, documentação OpenAPI e ambiente completo em Docker.

## 📋 Sobre o Projeto

O objetivo deste projeto é fornecer uma solução de backend eficiente para escritórios de advocacia ou tribunais, permitindo o controle do ciclo de vida de processos jurídicos. O sistema foi desenhado com foco em **integridade de dados**, **tratamento de erros semânticos** e **alta performance** utilizando Minimal APIs.

### Funcionalidades Principais

* **Gestão de Processos:** CRUD completo com validações de regras de negócio (CNJ).
* **Histórico Processual:** Registro e consulta de movimentações vinculadas.
* **Busca Especializada:** Pesquisa otimizada por Número Unificado (CNJ) e ID.
* **Validações de Integridade:** Prevenção de duplicidade de registros e *Cascade Delete* seguro.
* **Qualidade:** Testes de integração com xUnit rodando a cada push no GitHub Actions.

---

## 🛠️ Arquitetura e Decisões Técnicas

O projeto é uma API única construída com **Minimal APIs**, com os endpoints organizados por recurso, visando clareza e manutenibilidade.

### 1. Minimal APIs & Organização de Endpoints
Ao invés de *Controllers* tradicionais, utilizei **Minimal APIs** pela menor sobrecarga de memória e maior performance. O código foi refatorado para evitar um `Program.cs` inflado, utilizando **Extension Methods** para segregar as rotas:
* `Endpoints/ProcessosEndpoints.cs`
* `Endpoints/MovimentacoesEndpoints.cs`

### 2. Tratamento de Erros e Status HTTP
A API não expõe erros de banco de dados (500) para violações de regras de negócio.
* **Validação de entrada:** As *DataAnnotations* dos DTOs (como o padrão CNJ `0000000-00.0000.0.00.0000`) são validadas pela validação nativa de Minimal APIs do .NET 10 (`AddValidation`), e dados inválidos retornam `400 Bad Request` com os detalhes de cada campo.
* **Conflitos:** Tentativas de criar ou atualizar processos com números já existentes retornam `409 Conflict` com mensagens claras.
* **Not Found:** Buscas por IDs inexistentes retornam `404 Not Found`.

### 3. Integridade de Dados e EF Core
* **Relacionamentos:** Configuração `1:N` (Um Processo -> Muitas Movimentações).
* **Cascade Delete:** Configurado via Fluent API para garantir que, ao remover um processo, suas movimentações sejam limpas automaticamente, evitando "registros órfãos".
* **DTOs (Records):** Uso de `records` para garantir imutabilidade na transferência de dados.

---

## 📂 Estrutura do Projeto

A organização de pastas segue uma separação lógica de responsabilidades:

```text
ProcessosJudiciais.Api
├── 📂 Data          # Contexto do Banco de Dados (EF Core)
├── 📂 Dtos          # Objetos de Transferência de Dados (Input Models)
├── 📂 Endpoints     # Definição das Rotas (Separadas por domínio)
├── 📂 Models        # Entidades de Domínio
├── 📄 Program.cs    # Configuração de DI e Pipeline
└── 📄 requests.http # Requisições prontas para testar manualmente

tests/ProcessosJudiciais.Api.Tests
├── 📄 ApiFactory.cs                     # Sobe a API em memória com SQLite
├── 📄 ProcessosEndpointsTests.cs        # Testes dos endpoints de processos
└── 📄 MovimentacoesEndpointsTests.cs    # Testes dos endpoints de movimentações
```

---

## 🔧 Como Rodar o Projeto

### Opção 1: Docker (recomendado)

Sobe o SQL Server e a API juntos, já com o banco criado.

```bash
cp .env.example .env   # defina uma senha forte em SA_PASSWORD
docker compose up --build
```

* API: `http://localhost:5221`
* Documentação interativa (Swagger UI): `http://localhost:5221/swagger`

### Opção 2: Localmente

**Pré-requisitos:** .NET 10 SDK e um SQL Server rodando (local ou Docker).

Configure a string de conexão `DefaultConnection` no `appsettings.json`. Para criar as tabelas, rode a API com `Database__EnsureCreated=true` ou use o script abaixo:

```sql
CREATE DATABASE ProcessosJudiciaisDb;
GO
USE ProcessosJudiciaisDb;

CREATE TABLE Processos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Numero VARCHAR(25) NOT NULL UNIQUE,
    NomeAutor VARCHAR(100) NOT NULL,
    NomeReu VARCHAR(100) NOT NULL,
    DataCadastro DATETIME DEFAULT GETDATE()
);

CREATE TABLE Movimentacoes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProcessoId INT NOT NULL,
    TextoMovimentacao NVARCHAR(MAX) NOT NULL,
    DataMovimentacao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Processo_Movimentacoes FOREIGN KEY (ProcessoId) 
    REFERENCES Processos(Id) ON DELETE CASCADE
);
```

Depois, dentro da pasta `ProcessosJudiciais.Api`:

```bash
dotnet run
```

A API estará disponível em `http://localhost:5221` e o Swagger UI em `http://localhost:5221/swagger` (ambiente de desenvolvimento).

---

## ✅ Testes Automatizados

Os testes de integração sobem a API em memória com `WebApplicationFactory` e trocam o SQL Server por um **SQLite em memória**. O SQLite aplica de verdade o índice único e a exclusão em cascata, então as regras de integridade são testadas como no banco real, sem depender de infraestrutura.

```bash
dotnet test ProcessosJudiciais.slnx
```

Cenários cobertos:
* Criação, consulta, busca por número CNJ, atualização e exclusão de processos
* `400` para número fora do padrão CNJ e para movimentações com texto inválido
* `409` para número duplicado, na criação e na atualização
* `404` para processos inexistentes
* Exclusão em cascata das movimentações ao remover um processo

O pipeline do **GitHub Actions** compila, roda os testes e constrói a imagem Docker a cada push e pull request.

---

## 🧪 Testando Manualmente (API Client)
Além do Swagger UI, o projeto inclui um arquivo ```.http``` nativo para testar sem ferramentas externas (como Postman).

### Como usar:
1. Abra o arquivo ```requests.http``` no VS Code (com a extensão REST Client) ou Visual Studio 2022.
2. Clique em **Send Request** acima de cada chamada.

| Método  | Endpoint                          | Descrição                                              |
|--------|-----------------------------------|--------------------------------------------------------|
| GET    | `/processos`                      | Lista todos os processos (Resumo).                    |
| POST   | `/processos`                      | Cria um novo processo (Valida Regex CNJ).             |
| GET    | `/processos/{id}`                 | Busca detalhada (inclui movimentações).               |
| GET    | `/processos/busca/{numero}`       | Busca por número unificado.                           |
| PUT    | `/processos/{id}`                 | Atualiza dados (Valida duplicidade).                  |
| DELETE | `/processos/{id}`                 | Remove processo e histórico.                          |
| POST   | `/processos/{processoId}/movimentacoes`   | Adiciona nova movimentação a um processo.     |
| GET   | `/processos/{processoId}/movimentacoes`   | Lista as movimentações de um processo.         |

---

## 📝 Nota sobre o Frontend

Optei por focar meus esforços na construção de um **Backend sólido, seguro e bem estruturado**, em vez de entregar uma interface gráfica básica.

A interação com a API pode ser feita pelo **Swagger UI** (`/swagger`), por ferramentas como **Postman** e **Insomnia** ou pelo arquivo **`requests.http`** incluído no projeto, que já contém todos os cenários configurados.

---

Desenvolvido por **Róger Ferreira**: [LinkedIn](https://linkedin.com/in/rogergsferreira) · Licença [MIT](LICENSE)
