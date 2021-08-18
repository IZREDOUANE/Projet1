using Azure.Api.Data.DTOs;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Azure.Api.Data.DTOs.Email;
using Azure.Api.Data.Models;
using Azure.Api.Repository;
using System.Transactions;
using AutoMapper;
using System.Collections.Generic;
using MimeKit;
using HeyRed.Mime;
using MailKit.Net.Smtp;

namespace Azure.Api.Services
{
    public class MailService : IMailService
    {
        private const string NO_REPLY_EMAIL = "no_reply@lpcr.fr";

        private string _mailServer { get; set; }
        private int _serverPort { get; set; }
        private string _serverId { get; set; }
        private string _serverPwd { get; set; }

        private IMapper _mapper { get; set; }

        private IDocumentStoreRepository<Email> _mailRepo { get; set; }

        public MailService(IConfiguration configuration, IDocumentStoreRepository<Email> mailRepo, IMapper mapper)
        {
            _mailServer = configuration.GetSection("SendInBluecredentials").GetValue<string>("MailServer");
            _serverPort = Convert.ToInt32(configuration.GetSection("SendInBluecredentials").GetValue<string>("Port"));
            _serverId = configuration.GetSection("SendInBluecredentials").GetValue<string>("ServerIdentifiant");
            _serverPwd = configuration.GetSection("SendInBluecredentials").GetValue<string>("ServerPassword");

            _mailRepo = mailRepo;
            _mapper = mapper;
        }

        public async Task<bool> SendMail(EmailDTO email, IEnumerable<EmailDocumentDTO> documents = null)
        {
            try
            {
                email.From = string.IsNullOrEmpty(email.From) ? NO_REPLY_EMAIL : email.From;

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(email.FromName ?? "", email.From));
                message.To.Add(new MailboxAddress(email.ToName ?? "", email.To));
                message.Subject = email.Subject;

                var multipart = new Multipart("mixed");

                multipart.Add(new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = email.Body
                });

                if (documents != null)
                {
                    foreach (EmailDocumentDTO doc in documents)
                    {
                        var mime = MimeTypesMap.GetMimeType(doc.FileType).Split("/");


                        var attachment = new MimePart(mime[0], mime[1])
                        {
                            Content = new MimeContent(new MemoryStream(doc.Content)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = doc.Name
                        };

                        multipart.Add(attachment);
                    }
                }

                message.Body = multipart;

                using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var isMailHistorized = await SaveMailHistory(email);
                    if (!isMailHistorized)
                    {
                        return false;
                    }

                    var isMailSent = await SendAsync(message);
                    if (!isMailSent)
                    {
                        return false;
                    }

                    scope.Complete();
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> SendAsync(MimeMessage message)
        {
            try
            {
                using (var smtp = new SmtpClient())
                {
                    await smtp.ConnectAsync(_mailServer, _serverPort);
                    await smtp.AuthenticateAsync(_serverId, _serverPwd);
                    await smtp.SendAsync(message);
                    await smtp.DisconnectAsync(true);
                }
                    
                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> SaveMailHistory(EmailDTO email)
        {
            var emailToSave = _mapper.Map<Email>(email);

            try
            {
                emailToSave.SendingDate = DateTime.Now;
                var savedMail = await _mailRepo.Add(emailToSave);
                if (savedMail == null)
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
