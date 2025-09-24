using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Infrastructure.Repositories;

/// <summary>
/// SQLite implementation of the IPromptTemplateRepository
/// </summary>
public class SqlitePromptTemplateRepository : IPromptTemplateRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the SqlitePromptTemplateRepository
    /// </summary>
    public SqlitePromptTemplateRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Prompt Template Operations

    public async Task<PromptTemplate?> GetTemplateByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetAllTemplatesWithCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetTemplatesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.Category == categoryId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetTemplatesByCategoryWithCategoriesAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.Category == categoryId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<PromptTemplate>> GetTemplatesByTypeAsync(bool isSystemTemplate, CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.IsSystemTemplate == isSystemTemplate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllTemplatesAsync(cancellationToken);

        var term = searchTerm.ToLowerInvariant();
        return await _context.PromptTemplates
            .Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term) ||
                t.Content.ToLower().Contains(term))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> SearchTemplatesWithCategoriesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllTemplatesWithCategoriesAsync(cancellationToken);

        var term = searchTerm.ToLowerInvariant();
        return await _context.PromptTemplates
            .Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term) ||
                t.Content.ToLower().Contains(term))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetFavoriteTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.IsFavorite)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetFavoriteTemplatesWithCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.IsFavorite)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddTemplateAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.PromptTemplates.AddAsync(template, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateTemplateAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.PromptTemplates.Update(template);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteTemplateAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await GetTemplateByIdAsync(id, cancellationToken);
            if (template == null)
                return false;

            if (template.IsSystemTemplate)
                return false; // Cannot delete system templates

            _context.PromptTemplates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> IncrementTemplateUsageAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await GetTemplateByIdAsync(id, cancellationToken);
            if (template == null)
                return false;

            template.IncrementUsage();
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ToggleTemplateFavoriteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await GetTemplateByIdAsync(id, cancellationToken);
            if (template == null)
                return false;

            template.ToggleFavorite();
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region Prompt Category Operations

    public async Task<PromptCategory?> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.PromptCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PromptCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PromptCategories
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptCategory>> GetSystemCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PromptCategories
            .Where(c => SystemCategories.DefaultCategories.Any(sc => sc.Id == c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddCategoryAsync(PromptCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.PromptCategories.AddAsync(category, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateCategoryAsync(PromptCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.PromptCategories.Update(category);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await GetCategoryByIdAsync(id, cancellationToken);
            if (category == null)
                return false;

            // Check if there are templates using this category
            var templatesCount = await _context.PromptTemplates
                .Where(t => t.Category == category.Id)
                .CountAsync(cancellationToken);

            if (templatesCount > 0)
                return false; // Cannot delete category with templates

            _context.PromptCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateCategoryTemplateCountAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await GetCategoryByIdAsync(categoryId, cancellationToken);
            if (category == null)
                return false;

            var templateCount = await _context.PromptTemplates
                .Where(t => t.Category == categoryId)
                .CountAsync(cancellationToken);

            category.TemplateCount = templateCount;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region Statistics

    public async Task<TemplateStatistics> GetTemplateStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = new TemplateStatistics
        {
            TotalTemplates = await _context.PromptTemplates.CountAsync(cancellationToken),
            SystemTemplates = await _context.PromptTemplates.CountAsync(t => t.IsSystemTemplate, cancellationToken),
            UserTemplates = await _context.PromptTemplates.CountAsync(t => !t.IsSystemTemplate, cancellationToken),
            TotalCategories = await _context.PromptCategories.CountAsync(cancellationToken),
            TotalTemplateUsageCount = await _context.PromptTemplates.SumAsync(t => t.UsageCount, cancellationToken),
            MostUsedTemplateUsageCount = await _context.PromptTemplates.MaxAsync(t => (int?)t.UsageCount, cancellationToken) ?? 0,
            FavoriteTemplatesCount = await _context.PromptTemplates.CountAsync(t => t.IsFavorite, cancellationToken),
            LastUsedTemplateDate = await _context.PromptTemplates.MaxAsync(t => (DateTime?)t.LastUsedAt, cancellationToken)
        };

        return statistics;
    }

    public async Task<IEnumerable<PromptTemplate>> GetMostUsedTemplatesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.UsageCount > 0)
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromptTemplate>> GetRecentTemplatesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(t => t.LastUsedAt != null)
            .OrderByDescending(t => t.LastUsedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Initialization

    public async Task<bool> InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync(cancellationToken);

            // Add default system categories
            var existingCategories = await _context.PromptCategories.ToListAsync(cancellationToken);
            var existingCategoryIds = existingCategories.Select(c => c.Id).ToHashSet();

            foreach (var systemCategory in SystemCategories.DefaultCategories)
            {
                if (!existingCategoryIds.Contains(systemCategory.Id))
                {
                    await _context.PromptCategories.AddAsync(systemCategory, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Template type enum
/// </summary>
public enum TemplateType
{
    System = 0,
    User = 1
}