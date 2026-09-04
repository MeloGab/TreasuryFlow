# Roadmap do TreasuryFlow

## Versão 1

O objetivo da V1 é entregar uma aplicação arquiteturalmente coerente, funcional, executável, testada, documentada e explicável. Ela não pretende representar uma plataforma pronta para produção.

O projeto continua em evolução. A documentação da V1 registra o estado atual da aplicação, mas não impede a inclusão de novas funcionalidades ou melhorias em entregas posteriores.

### Documentação técnica

- Manter a documentação técnica em `docs/TREASURYFLOW-TECHNICAL-DOCUMENTATION.md` atualizada conforme o projeto evoluir.

## Observabilidade distribuída

A adoção de OpenTelemetry, exportação OTLP e Aspire Dashboard foi avaliada e planejada para uma entrega futura.

O Aspire Dashboard pode ser executado localmente de forma independente. Entretanto, instrumentar apenas as requisições HTTP, o acesso ao banco de dados e o Worker produziria rastreamentos isolados e não representaria corretamente o fluxo completo de uma ordem de pagamento.

O Outbox cria uma fronteira durável e assíncrona entre a requisição da API e a publicação no RabbitMQ. Quando o evento é publicado, a atividade original da requisição HTTP já terminou. Por isso, uma implementação correta deverá tratar explicitamente a correlação através dessa fronteira.

### Escopo mínimo da implementação futura

- Instrumentar API, SQL Server, processador do Outbox, RabbitMQ, Worker e armazenamento de comprovantes.
- Persistir ou vincular corretamente o contexto de rastreamento na fronteira do Outbox.
- Propagar o contexto pelos headers das mensagens do RabbitMQ.
- Preservar a correlação em tentativas de reprocessamento e mensagens com falha.
- Exportar a telemetria por OTLP.
- Usar o Aspire Dashboard standalone para visualização local.
- Cobrir a propagação e os cenários de falha com testes.

Essa decisão evita adicionar uma observabilidade apenas decorativa. A implementação será retomada em uma entrega própria, com o encadeamento completo tratado corretamente.
