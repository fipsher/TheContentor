* Inject DbContext directly. Do not use repository abstraction or any custom interfaces.
* In TheContentor.Application, if any model is required, it must be added to Models folder, next to Commands and Queries
* All API routes must start with /api/
* No authorization is required. This is ONLY internal project that does not have secure information.