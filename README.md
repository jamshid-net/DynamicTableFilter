# DynamicTableFilter

[![NuGet version](https://img.shields.io/nuget/v/DynamicTableFilter.svg)](https://www.nuget.org/packages/DynamicTableFilter)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**DynamicTableFilter** is a lightweight, powerful, and highly flexible library for **EF Core** and `.NET`. It allows you to dynamically apply filtering, sorting, and pagination to any `IQueryable<T>` data source based on JSON payloads from HTTP requests.

Under the hood, it uses **Expression Trees** to safely translate user-defined filters directly into highly optimized SQL queries, preventing SQL injection and avoiding performance bottlenecks.

---

## 🌟 Features

- **Zero Configuration:** Drop it in and it works immediately with your existing Entity Framework Core setup.
- **100% Universal:** Seamlessly works with all EF Core database providers (PostgreSQL, SQL Server, MySQL, SQLite) **AND** In-Memory collections (`List<T>.AsQueryable()`) for effortless unit testing and mock data.
- **Dynamic Expression Trees:** No hardcoded `Where` clauses. The library reads the JSON payload and builds the exact SQL needed.
- **Strongly Typed & Safe:** Validates property names (DTO projection) before applying filters. It throws clear, developer-friendly English exceptions if keys don't match or types cannot be converted.
- **Advanced Filtering:** Supports exact matches, range filtering (`.from`/`.to`), array `IN` clauses (for both numbers and strings), boolean shorthands (`0` / `1`), and `LIKE` string searches.

---

## 📦 Installation

Install via .NET CLI:
```bash
dotnet add package DynamicTableFilter
```

---

## 🚀 Quick Start (Backend)

Integrating **DynamicTableFilter** into your API is incredibly simple. You only need to call `.ApplyPageRequest(request)` on your `IQueryable`.

```csharp
using DynamicTableFilter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context) => _context = context;

    [HttpPost("list")]
    public async Task<IActionResult> GetUsers([FromBody] FilterRequest request)
    {
        // 1. Get base query (Do NOT call .ToList() yet)
        IQueryable<User> query = _context.Users.AsNoTracking();

        // Dynamically apply Filters, Sorting, Pagination, and safely get the Total Count
        var result = await query.ApplyPageRequestAsync(request);

        // Return Paged Response
        return Ok(new 
        {
            Data = result.Data,
            TotalCount = result.TotalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        });
    }
}
```

---

## 🌐 Frontend Usage (How to send requests)

The frontend sends a JSON payload to the backend. The library automatically parses this payload and builds the SQL query.

> ⚠️ **CRITICAL NOTE ON PROJECTION:** The `key` in the filter payload **must exactly match** the property name in the backend C# class (Entity or DTO). It is case-sensitive! If the key or value type is incorrect, the library will throw a precise `ArgumentException`.

### 1. Base Payload Structure

```json
{
  "pageIndex": 0,       // 0-based index
  "pageSize": 10,       // Items per page
  "sort": [
    {
      "key": "CreatedAt",
      "value": 1        // 0 = Ascending, 1 = Descending
    }
  ],
  "filter": [
    {
      "key": "Role",
      "value": "Admin"
    }
  ]
}
```

### 2. Supported Filter Types

#### A. String Filtering (Contains)
Translated to SQL `LIKE` (`%value%`). Case-insensitive search.
```json
{ "key": "FullName", "value": "john" }
```

#### B. Exact Matches (Numbers, Booleans, Enums)
Translates to SQL `=`. For Booleans, you can send `true`/`false` or shorthand `1`/`0`.
```json
{ "key": "Age", "value": 30 },
{ "key": "IsVerified", "value": 1 },  // Same as true
{ "key": "IsActive", "value": false }, // Same as 0
{ "key": "Status", "value": 1 }
```

#### C. Multiple Values (IN Clause or OR conditions)
Pass an array of values to search multiple options. 
- **Numeric Arrays:** Translates to highly optimized SQL `IN (1, 3, 5)`.
- **String Arrays:** Translates to `(Name LIKE '%John%') OR (Name LIKE '%Alex%')`.
```json
{ "key": "Id", "value": [1, 3, 5] },
{ "key": "DepartmentId", "value": [2, 5, 8] },
{ "key": "CategoryName", "value": ["Electronics", "Books"] }
```

#### D. Range Filters (From / To)
Append `.from` (>=) or `.to` (<) to the exact property key. Fully supported for **Dates** and **ALL Numeric types** (`int`, `decimal`, `double`, `float`, `long`).
```json
{ "key": "Salary.from", "value": 50000.00 },
{ "key": "Salary.to", "value": 120000.00 },
{ "key": "CreatedAt.from", "value": "01.01.2024" },
{ "key": "CreatedAt.to", "value": "12.31.2024" }
```
*(Date formats supported by default: `MM.dd.yyyy`, `yyyy-MM-dd`, `yyyy-MM-ddTHH:mm:ss`)*

#### E. Nested Properties (Navigation Properties)
You can easily filter or sort by nested properties using dot notation (`.`).
```json
{ "key": "Department.Name", "value": "IT" },
{ "key": "Author.Address.City", "value": "Tashkent" }
```

#### F. Array/Collection Properties
If the C# property itself is an array (e.g., `public int[] Tags { get; set; }`), sending a single value checks if the array contains that value natively in SQL (`ANY`).
```json
{ "key": "Tags", "value": 42 }
```

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! 
Feel free to check the [issues page](https://github.com/jamshid-net/DynamicTableFilter/issues).

## 📄 License

This project is [MIT](https://opensource.org/licenses/MIT) licensed.
