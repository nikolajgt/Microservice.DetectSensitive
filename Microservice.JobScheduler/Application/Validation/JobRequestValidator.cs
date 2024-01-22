using FluentValidation;
using Microservice.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.JobScheduler.Application.Validation;

internal class JobRequestValidator : AbstractValidator<JobRequest>
{
    public JobRequestValidator()
    {
        RuleFor(task => task.HarvesterType).NotEmpty();
    }
}
