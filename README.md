# DynamicTableFilter

[![NuGet version](https://img.shields.io/nuget/v/DynamicTableFilter.svg)](https://www.nuget.org/packages/DynamicTableFilter)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**DynamicTableFilter** is a lightweight, powerful, and highly flexible library for **EF Core** and `.NET`. It allows you to dynamically apply filtering, sorting, and pagination to any `IQueryable<T>` data source based on JSON payloads from HTTP requests.

Under the hood, it uses **Expression Trees** to safely translate user-defined filters directly into highly optimized SQL queries, preventing SQL injection and avoiding performance bottlenecks.

---

## 🌟 Features

- **Zero Configuration:** Drop it in and it works immediately with your existing Entity Framework Core setup.
- **Dynamic Expression Trees:** No hardcoded `Where` clauses. The library reads the JSON payload and builds the exact SQL needed.
- **Cross-Database Compatible:** Works seamlessly with SQL Server, PostgreSQL, MySQL, and SQLite (uses `EF.Functions.Like` for string matching).
- **Strongly Typed & Safe:** Validates property names (DTO projection) before applying filters, throwing clear exceptions if types mismatch.
- **Advanced Filtering:** Supports exact matches, range filtering (`from`/`to`), `IN` clauses (arrays), and `LIKE` string searches.

---

## 📦 Installation

*(Assuming the package is published to NuGet in the future)*

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

        // 2. Count total records (before pagination)
        int totalCount = await query.CountAsync();

        // 3. Apply Filters, Sorting, and Pagination dynamically
        query = query.ApplyPageRequest(request);

        // 4. Execute SQL
        var items = await query.ToListAsync();

        // 5. Return Paged Response
        return Ok(new 
        {
            Data = items,
            TotalCount = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        });
    }
}
```

---

## 💻 Frontend Usage (How to send requests)

The frontend sends a JSON payload to the backend. The library automatically parses this payload and builds the SQL query.

> ⚠️ **CRITICAL NOTE ON PROJECTION:** The `key` in the filter payload **must exactly match** the property name in the backend C# class (Entity or DTO). It is case-sensitive!

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
Translates to SQL `=`.
```json
{ "key": "Age", "value": 30 },
{ "key": "IsActive", "value": true },
{ "key": "Status", "value": 1 }
```

#### C. Multiple Values (IN Clause)
Translates to SQL `IN (...)`. Pass an array of values.
```json
{ "key": "DepartmentId", "value": [2, 5, 8] }
```

#### D. Range Filters (From / To)
Append `.from` (>=) or `.to` (<) to the property key. Perfect for dates and numeric ranges.
```json
{ "key": "Price.from", "value": 100.00 },
{ "key": "Price.to", "value": 500.00 },
{ "key": "CreatedAt.from", "value": "01.01.2024" },
{ "key": "CreatedAt.to", "value": "12.31.2024" }
```
*(Date formats supported by default: `MM.dd.yyyy`, `yyyy-MM-dd`, `yyyy-MM-ddTHH:mm:ss`)*

#### E. Array/Collection Filtering
If the C# property is an array (e.g., `public int[] Tags { get; set; }`), sending a single value checks if the array contains that value.
```json
{ "key": "Tags", "value": 42 }
```

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! 
Feel free to check the [issues page](https://github.com/jamshid-net/DynamicTableFilter/issues).

## 📝 License

This project is [MIT](https://opensource.org/licenses/MIT) licensed.
