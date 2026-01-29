# ⚖️ API de Gestão de Processos Judiciais

![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=.net&logoColor=white)

API RESTful robusta e escalável desenvolvida para o gerenciamento de processos judiciais e histórico de movimentações utilizando o ecossistema .NET.

## 📋 Sobre o Projeto

O objetivo deste projeto é fornecer uma solução de backend eficiente para escritórios de advocacia ou tribunais, permitindo o controle do ciclo de vida de processos jurídicos. O sistema foi desenhado com foco em **integridade de dados**, **tratamento de erros semânticos** e **alta performance** utilizando Minimal APIs.

### Funcionalidades Principais

* **Gestão de Processos:** CRUD completo com validações de regras de negócio (CNJ).
* **Histórico Processual:** Registro e consulta de movimentações vinculadas.
* **Busca Especializada:** Pesquisa otimizada por Número Unificado (CNJ) e ID.
* **Validações de Integridade:** Prevenção de duplicidade de registros e *Cascade Delete* seguro.

---

## 🛠️ Arquitetura e Decisões Técnicas

O projeto foi estruturado seguindo o padrão de **Modular Monolith** com **Minimal APIs**, visando clareza e manutenibilidade.

### 1. Minimal APIs & Organização de Endpoints
Ao invés de *Controllers* tradicionais, utilizei **Minimal APIs** pela menor sobrecarga de memória e maior performance. O código foi refatorado para evitar um `Program.cs` inflado, utilizando **Extension Methods** para segregar as rotas:
* `Endpoints/ProcessosEndpoints.cs`
* `Endpoints/MovimentacoesEndpoints.cs`

### 2. Tratamento de Erros e Status HTTP
A API não expõe erros de banco de dados (500) para violações de regras de negócio.
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
└── 📄 requests.http # Arquivo de Testes de Integração
```

---

## 🔧 Como Rodar o Projeto

### Pré-requisitos
* .NET SDK instalado.
* SQL Server rodando (Local ou Docker).

### 1. Configuração do Banco de Dados

Configure a string de conexão no ```appsettings.json``` ou utilize o script SQL abaixo para criar a estrutura manualmente:

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

### 2. Rodando a aplicação

No terminal, dentro da pasta do projeto:

```bash
dotnet restore
dotnet run
```

A API estará disponível em ```http://localhost:5221```.

---

## 🧪 Testes e Documentação (API Client)
Para facilitar os testes sem a necessidade de ferramentas externas (como Postman), o projeto inclui um arquivo ```.http``` nativo.

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

A interação com a API deve ser feita através de ferramentas como **Postman**, **Insomnia** ou, preferencialmente, utilizando o arquivo **`requests.http`** incluído na raiz deste projeto, que já contém todos os cenários de teste configurados.

---

Desenvolvido por **Róger Ferreira**: [Linkedin](https://linkedin.com/in/rogergsferreira)
