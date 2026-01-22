* Inject DbContext directly. Do not use repository abstraction or any custom interfaces.
* In TheContentor.Application, if any model is required, it must be added to Models folder, next to Commands and Queries
* All API routes must start with /api/
* No authorization is required. This is ONLY internal project that does not have secure information.
* Video assets can be very big. And streams should be used.
* If entity can be activated/deactivated, then you cannot set IsActive as part of the Create/Update command. Only in the Activate/Deactivate/Toggle commands. ByDefault IsActive is true.
* If API is being changed, the UI also must be changed accordingly. And vice versa.
* Use FluentValidation for validation.
* Use C# 10 features like file scoped namespaces, top-level statements, and target-typed new expressions.
* Prefer var over explicit types when possible.
* Use async/await whenever possible.