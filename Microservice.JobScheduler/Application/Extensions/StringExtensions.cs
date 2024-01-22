using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.JobScheduler.Application.Extensions;

public static class StringExtensions
{
    public static bool ContainsAny(this string s, IEnumerable<string> substrings)
    {
        ArgumentException.ThrowIfNullOrEmpty(s, nameof(s));
        return substrings.Any(substring => s.Contains(substring, StringComparison.CurrentCultureIgnoreCase));
    }

    public static bool IsDue(this string s, DateTime? date)
    {
        if (date is null)
            return true;

        var cronExpression = CronExpression.Parse(s);
        return cronExpression
          .GetNextOccurrence(DateTime.SpecifyKind(date.Value, DateTimeKind.Utc)) <= DateTime.UtcNow;
    }
}

