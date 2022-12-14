 # Code Organization

## Projects
 The codebase is designed to support different data stores, identity providers, operating systems, and is not tied to any particular cloud or hosting environment. To achieve these goals, the project is broken down into layers:

| Layer              | Example                                                      | Comments                                                                              |
| ------------------ | ------------------------------------------------------------ |---------------------------------------------------------------------------------------|
| Hosting Layer      | `Microsoft.Health.Dicom.Web`                                 | Supports hosting in different environments with custom configuration of IoC container. For development purposes only. |
| REST API Layer     | `Microsoft.Health.Dicom.Api`                                 | Implements the RESTful DICOMweb&trade; |
| Core Logic Layer   | `Microsoft.Health.Dicom.Core`                                | Implements core logic to support DICOMweb&trade; |
| Persistence Layer  | `Microsoft.Health.Dicom.Sql` `Microsoft.Health.Dicom.Blob`   | Pluggable persistence provider |

## Patterns

Dicom server code follows the below **patterns** to organize code in these layers.

### [MediatR Handler](https://github.com/jbogard/MediatR):

<em>Used to dispatch messages from the Controller methods. Used to transform requests and responses from the hosting layer to the service.</em>
- Responsibilities: authorization decisions, message deserialization
- Naming Guidelines: `Resource`Handler
-  Example: [DeleteHandler](/src/Microsoft.Health.Dicom.Core/Features/Delete/DeleteHandler.cs)

### [Resource Service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-6.0): 
<em>Used to implement business logic. Like input validation(inline or call), orchestration, or core response objects.</em>
- Responsibilities: implementing Service Class Provider responsibilities, including orchestrating persistence providers.
- Keep the services scoped to the resource operations.
- Naming Guidelines: `Resource`Service
-  Example: [IQueryService](/src/Microsoft.Health.Dicom.Core/Features/Query/IQueryService.cs)

### [Store Service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-6.0):
<em>Data store specific implementation of storing/retrieving/deleting the data.</em>
- Responsibilities: provide an abstraction to a single persistence store.
- The interface is defined in the core and implementation in the specific persistence layer.
- They should not be accessed outside a service.
- Naming Guidelines: `Resource`Store
- Example: [SqlIndexDataStore](/src/Microsoft.Health.Dicom.SqlServer/Features/Store/SqlIndexDataStore.cs)

### [Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-6.0):
 <em>Standard/Common concerns like authentication, routing, logging, exception handling that needs to be done for each request, are separated into their own components.</em>

- Naming Guidelines: `Responsibility`Middleware.
- Example: [ExceptionHandlingMiddleware](/src/Microsoft.Health.Dicom.Api/Features/Exceptions/ExceptionHandlingMiddleware.cs).

### [Action Filters](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-6.0):
<em>Dicom code uses pre-action filters. Authorization filters for authentication and Custom filter for acceptable content-type validation.</em>

- Naming Guidelines: `Responsibility`FilterAttribute.
- Example: [AcceptContentFilterAttribute](/src/Microsoft.Health.Dicom.Api/Features/Filters/AcceptContentFilterAttribute.cs).
