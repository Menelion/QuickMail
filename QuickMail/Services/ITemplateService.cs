using System.Collections.Generic;
using System.Threading.Tasks;
using QuickMail.Models;

namespace QuickMail.Services;

public interface ITemplateService
{
    Task<List<MessageTemplate>> LoadAllAsync();
    Task<MessageTemplate> AddAsync(MessageTemplate item);
    Task UpdateAsync(MessageTemplate item);
    Task DeleteAsync(int id);
}
