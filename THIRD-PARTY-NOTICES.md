# Avis relatifs aux composants tiers — granit-iot

Ce fichier répertorie les bibliothèques tierces utilisées par le projet
**granit-iot** ainsi que leurs licences respectives. Il est mis à jour
à chaque ajout ou modification de dépendance externe.

Dernière mise à jour : 2026-04-17

---

## Récapitulatif des licences

| Licence      | Nombre de packages |
| ------------ | ------------------ |
| Apache-2.0   | 31                 |
| MIT          | 6                  |
| BSD-3-Clause | 2                  |
| PostgreSQL   | 1                  |

Toutes les licences sont permissives et compatibles avec la distribution
Apache-2.0 du projet granit-iot.

---

## Dépendances de production

### Granit framework (Apache-2.0)

Packages internes publiés depuis `granit-fx/granit-dotnet`.

| Package                                | Version     | Copyright             |
| -------------------------------------- | ----------- | --------------------- |
| Granit                                 | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Authorization                   | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Diagnostics                     | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Encryption                      | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Events.Wolverine                | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Guids                           | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Http.ApiDocumentation           | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Http.Idempotency                | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.MultiTenancy                    | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Persistence                     | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Persistence.EntityFrameworkCore | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.RateLimiting                    | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Settings                        | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Timeline                        | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Timing                          | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Validation                      | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Wolverine                       | 0.1.0-dev.* | (c) Digital Dynamics  |
| Granit.Workflow                        | 0.1.0-dev.* | (c) Digital Dynamics  |

### Apache-2.0 (tiers)

| Package                                        | Version | Copyright                                                   |
| ---------------------------------------------- | ------- | ----------------------------------------------------------- |
| AWSSDK.Core                                    | 4.0.*   | Copyright (c) Amazon.com, Inc. or its affiliates            |
| AWSSDK.IoT                                     | 4.0.*   | Copyright (c) Amazon.com, Inc. or its affiliates            |
| AWSSDK.IotData                                 | 4.0.*   | Copyright (c) Amazon.com, Inc. or its affiliates            |
| AWSSDK.SecretsManager                          | 4.0.*   | Copyright (c) Amazon.com, Inc. or its affiliates            |
| FluentValidation                               | 12.*    | Copyright (c) Jeremy Skinner, .NET Foundation 2008-2025     |
| FluentValidation.DependencyInjectionExtensions | 12.*    | Copyright (c) Jeremy Skinner, .NET Foundation 2008-2025     |

### MIT

| Package                      | Version | Copyright                 |
| ---------------------------- | ------- | ------------------------- |
| Microsoft.AspNetCore.OpenApi | 10.*    | (c) Microsoft Corporation |
| WolverineFx                  | 5.31.*  | JasperFx Contributors     |

### PostgreSQL License

| Package                               | Version | Copyright                                  |
| ------------------------------------- | ------- | ------------------------------------------ |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.*    | Copyright 2025 The Npgsql Development Team |

> La licence PostgreSQL est une licence permissive de style BSD-2, compatible
> avec une redistribution Apache-2.0.

---

## Dépendances de test uniquement

### MIT (tests)

| Package                              | Version | Copyright                       |
| ------------------------------------ | ------- | ------------------------------- |
| Bogus                                | 35.*    | Copyright (c) 2015 Brian Chavez |
| coverlet.collector                   | 8.*     | (c) 2018 Toni Solarin-Sodara    |
| Microsoft.EntityFrameworkCore.Sqlite | 10.*    | (c) Microsoft Corporation       |
| Microsoft.NET.Test.Sdk               | 18.*    | (c) Microsoft Corporation       |

### Apache-2.0 (tests)

| Package                               | Version     | Copyright                                              |
| ------------------------------------- | ----------- | ------------------------------------------------------ |
| Granit.ArchitectureTests.Abstractions | 0.1.0-dev.* | (c) Digital Dynamics                                   |
| TngTech.ArchUnitNET                   | 0.13.*      | Copyright (c) 2019-2025 TNG Technology Consulting GmbH |
| TngTech.ArchUnitNET.xUnit             | 0.13.*      | Copyright (c) 2019-2025 TNG Technology Consulting GmbH |
| xunit.v3                              | 3.*         | Copyright (C) .NET Foundation                          |
| xunit.runner.visualstudio             | 3.*         | Copyright (C) .NET Foundation                          |

### BSD-3-Clause

| Package     | Version | Copyright                                |
| ----------- | ------- | ---------------------------------------- |
| NSubstitute | 5.*     | NSubstitute Contributors                 |
| Shouldly    | 4.*     | Copyright (c) 2017 Shouldly Contributors |

---

## Notes

- Les versions avec `*` suivent le versionnement flottant centralisé dans
  `Directory.Packages.props`.
- Les licences et attributions sont alignées avec
  [granit-dotnet/THIRD-PARTY-NOTICES.md](https://github.com/granit-fx/granit-dotnet/blob/main/THIRD-PARTY-NOTICES.md)
  pour toute dépendance déjà présente en amont.
- Aucune dépendance sous licence copyleft (GPL, LGPL, AGPL, SSPL) n'est
  utilisée. Toute nouvelle dépendance sous licence non permissive doit
  être signalée avant intégration, conformément aux règles du repo.
