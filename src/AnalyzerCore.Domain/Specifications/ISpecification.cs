using System.Linq.Expressions;

namespace AnalyzerCore.Domain.Specifications;

/// <summary>
/// Represents a specification pattern for building type-safe queries.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// The criteria expression for filtering.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Includes for eager loading navigation properties.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String-based includes for nested navigation properties.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression (ascending).
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression (descending).
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Number of records to take.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Number of records to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Whether paging is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }
}
