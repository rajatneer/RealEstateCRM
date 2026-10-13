using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;

namespace RealEstateCRM.Services
{
    public interface IPersonService
    {
        Task<List<Person>> GetAllAsync();
        Task<Person> CreateAsync(string name);
        Task<bool> DeleteAsync(int id);
    }

    public class PersonService : IPersonService
    {
        private static readonly string[] Starter = { "Nakul", "Ridhima", "Manjunath" };
        private readonly CrmDbContext _context;

        public PersonService(CrmDbContext context) => _context = context;

        public async Task<List<Person>> GetAllAsync()
        {
            // First visit: start the master list with the people named on the stage list.
            if (!await _context.People.AnyAsync())
            {
                foreach (var n in Starter) _context.People.Add(new Person { Name = n });
                await _context.SaveChangesAsync();
            }
            return await _context.People.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<Person> CreateAsync(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) throw new BadRequestException("Name is required.");
            if (name.Length > 100) throw new BadRequestException("Name must be 100 characters or fewer.");

            var existing = await _context.People.FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
            if (existing != null) return existing;

            var person = new Person { Name = name };
            _context.People.Add(person);
            await _context.SaveChangesAsync();
            return person;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.Id == id);
            if (person == null) return false;
            _context.People.Remove(person);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
