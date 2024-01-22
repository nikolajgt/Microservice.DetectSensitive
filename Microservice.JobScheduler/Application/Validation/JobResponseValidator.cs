using FluentValidation;
using Microservice.Domain;


namespace Microservice.JobScheduler.Application.Validation;

internal class JobResponseValidator : AbstractValidator<JobResponse>
{
    public JobResponseValidator()
    {
        RuleFor(task => task.JobHistoryId).NotEmpty();
        RuleFor(task => task.Address).NotEmpty();
    }
}
