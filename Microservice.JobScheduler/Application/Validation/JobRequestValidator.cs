using FluentValidation;
using Microservice.Domain;

namespace Microservice.JobScheduler.Application.Validation;

internal class JobRequestValidator : AbstractValidator<JobRequest>
{
    public JobRequestValidator()
    {
        RuleFor(task => task.HarvesterType).NotEmpty();
    }
}
