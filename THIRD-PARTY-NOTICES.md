# Avis relatifs aux composants tiers — granit-iot

Ce fichier repertorie les bibliotheques tierces utilisees par le projet
**granit-iot** ainsi que leurs licences respectives. Il est mis a jour
a chaque ajout ou modification de dependance externe.

Derniere mise a jour : 2026-04-16

---

## Recapitulatif des licences

| Licence      | Nombre de packages |
| ------------ | ------------------ |
| Apache-2.0   | 26                 |
| MIT          | 7                  |
| BSD-2-Clause | 1                  |
| BSD-3-Clause | 1                  |
| PostgreSQL   | 1                  |

Toutes les licences sont permissives et compatibles avec la distribution
Apache-2.0 du projet granit-iot.

---

## Dependances de production

### Granit framework (Apache-2.0)

Packages internes publies depuis `granit-fx/granit-dotnet`.
Copyright (c) Digital Dynamics.

| Package                                | Version     | Licence    |
| -------------------------------------- | ----------- | ---------- |
| Granit                                 | 0.1.0-dev.* | Apache-2.0 |
| Granit.Authorization                   | 0.1.0-dev.* | Apache-2.0 |
| Granit.Diagnostics                     | 0.1.0-dev.* | Apache-2.0 |
| Granit.Encryption                      | 0.1.0-dev.* | Apache-2.0 |
| Granit.Events.Wolverine                | 0.1.0-dev.* | Apache-2.0 |
| Granit.Guids                           | 0.1.0-dev.* | Apache-2.0 |
| Granit.Http.ApiDocumentation           | 0.1.0-dev.* | Apache-2.0 |
| Granit.Http.Idempotency                | 0.1.0-dev.* | Apache-2.0 |
| Granit.MultiTenancy                    | 0.1.0-dev.* | Apache-2.0 |
| Granit.Persistence                     | 0.1.0-dev.* | Apache-2.0 |
| Granit.Persistence.EntityFrameworkCore | 0.1.0-dev.* | Apache-2.0 |
| Granit.RateLimiting                    | 0.1.0-dev.* | Apache-2.0 |
| Granit.Settings                        | 0.1.0-dev.* | Apache-2.0 |
| Granit.Timeline                        | 0.1.0-dev.* | Apache-2.0 |
| Granit.Timing                          | 0.1.0-dev.* | Apache-2.0 |
| Granit.Validation                      | 0.1.0-dev.* | Apache-2.0 |
| Granit.Wolverine                       | 0.1.0-dev.* | Apache-2.0 |
| Granit.Workflow                        | 0.1.0-dev.* | Apache-2.0 |

### Validation (Apache-2.0)

Copyright (c) .NET Foundation and contributors.

| Package                                        | Version | Licence    |
| ---------------------------------------------- | ------- | ---------- |
| FluentValidation                               | 12.*    | Apache-2.0 |
| FluentValidation.DependencyInjectionExtensions | 12.*    | Apache-2.0 |

### ASP.NET Core / Microsoft (MIT)

Copyright (c) .NET Foundation and contributors.

| Package                      | Version | Licence |
| ---------------------------- | ------- | ------- |
| Microsoft.AspNetCore.OpenApi | 10.*    | MIT     |

### Messaging / CQRS (MIT)

Copyright (c) Jeremy D. Miller, Team JasperFx.

| Package     | Version | Licence |
| ----------- | ------- | ------- |
| WolverineFx | 5.31.*  | MIT     |

### PostgreSQL data provider (PostgreSQL License)

Copyright (c) The Npgsql Development Team.

| Package                               | Version | Licence    |
| ------------------------------------- | ------- | ---------- |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.*    | PostgreSQL |

> La licence PostgreSQL est une licence permissive de style BSD-2, compatible
> avec une redistribution Apache-2.0.

---

## Dependances de test uniquement

### Frameworks de test (Apache-2.0 / MIT)

| Package                   | Version | Licence    | Copyright                                         |
| ------------------------- | ------- | ---------- | ------------------------------------------------- |
| xunit.v3                  | 3.*     | Apache-2.0 | Copyright (c) .NET Foundation, xUnit contributors |
| xunit.runner.visualstudio | 3.*     | MIT        | Copyright (c) .NET Foundation, xUnit contributors |
| Microsoft.NET.Test.Sdk    | 18.*    | MIT        | Copyright (c) Microsoft Corporation               |
| coverlet.collector        | 8.*     | MIT        | Copyright (c) .NET Foundation                     |

### Assertions & mocks

| Package     | Version | Licence      | Copyright                              |
| ----------- | ------- | ------------ | -------------------------------------- |
| Shouldly    | 4.*     | BSD-2-Clause | Copyright (c) Shouldly contributors    |
| NSubstitute | 5.*     | BSD-3-Clause | Copyright (c) NSubstitute contributors |

### Donnees synthetiques (MIT)

| Package | Version | Licence |
| ------- | ------- | ------- |
| Bogus   | 35.*    | MIT     |

> Copyright (c) Brian Chavez.

### Tests d'architecture (Apache-2.0)

| Package                               | Version     | Licence    |
| ------------------------------------- | ----------- | ---------- |
| TngTech.ArchUnitNET                   | 0.13.*      | Apache-2.0 |
| TngTech.ArchUnitNET.xUnit             | 0.13.*      | Apache-2.0 |
| Granit.ArchitectureTests.Abstractions | 0.1.0-dev.* | Apache-2.0 |

> ArchUnitNET : Copyright (c) TNG Technology Consulting GmbH.

### EF Core provider SQLite (MIT)

Utilise uniquement comme base en memoire pour les tests d'integration EF Core.
Copyright (c) .NET Foundation and contributors.

| Package                              | Version | Licence |
| ------------------------------------ | ------- | ------- |
| Microsoft.EntityFrameworkCore.Sqlite | 10.*    | MIT     |

---

## Notes

- Les versions avec `*` suivent le versionnement flottant centralise dans
  `Directory.Packages.props`.
- Aucune dependance sous licence copyleft (GPL, LGPL, AGPL, SSPL) n'est
  utilisee. Toute nouvelle dependance sous licence non permissive doit
  etre signalee avant integration, conformement aux regles du repo.
- Les transitions de licences (rotation, changement amont) sont surveillees
  lors des mises a jour de dependances.
