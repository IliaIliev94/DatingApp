using System.Linq;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
            
        }
        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FindAsync(id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages
                .OrderByDescending(message => message.DateSent)
                .AsQueryable();

            query = messageParams.Container switch 
            {
                "Inbox" => query.Where(user => user.RecipientUsername == messageParams.Username 
                    && user.RecipientDeleted == false),
                "Outbox" => query.Where(user => user.SenderUsername == messageParams.Username 
                    && user.SenderDeleted == false),
                _ => query.Where(user => user.RecipientUsername == messageParams.Username 
                    && user.RecipientDeleted == false && user.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDto>
                .CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, 
            string recipientUsername)
        {
            var messages = await _context.Messages
                .Include(user => user.Sender).ThenInclude(photo => photo.Photos)
                .Include(user => user.Sender).ThenInclude(photo => photo.Photos)
                .Where(message => (message.RecipientUsername == currentUsername 
                    && message.SenderUsername == recipientUsername && message.RecipientDeleted == false) ||
                    (message.RecipientUsername == recipientUsername 
                    && message.SenderUsername == currentUsername && message.SenderDeleted == false))
                .OrderBy(message => message.DateSent)
                .ToListAsync();

            var unreadMessages = messages.Where(message => message.DateRead == null 
                && message.RecipientUsername == currentUsername).ToList();

            if(unreadMessages.Any()) 
            {
                foreach(var message in unreadMessages) 
                {
                    message.DateRead = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}