using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.Contact;
using Azure.Api.Data.DTOs.SalesForce;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace Azure.Api.Services
{
    public class AccountFamilleService : IAccountFamilleService
    {
        private readonly ISalesForceService _salesForceService;
        public readonly IConfiguration configuration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceUser"></param>
        /// <param name="userRepository"></param>
        /// <param name="serviceMail"></param>
        /// <param name="serviceSalesForce"></param>
        /// <param name="configuration"></param>
        public AccountFamilleService(ISalesForceService serviceSalesForce, IConfiguration configuration)
        {
            this._salesForceService = serviceSalesForce;
            this.configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        public AccountFamilleService()
        {
        }

        /// <summary>
        /// Permet de céer un contact (Parent ou Famille) pour un compte famille
        /// </summary>
        /// <param name="sfID">l'ID du compte famille sur SalesForce</param>
        /// <param name="contact">le contact à ajouter</param>
        /// <returns>le contact ajouté avec son ID inclut</returns>
        public async Task<ContactParentEnfantDTO> CreateAccountContactPE(string sfID, ContactParentEnfantDTO contact)
        {
            try
            {
                var response = await _salesForceService.CreateContactPEAsync(sfID, contact);
                if (response == null)
                {
                    throw new Exception(String.Format(
                        "Failed to create a new contact {0} {1} for sfID: {2}",
                        contact.Prenom,
                        contact.Nom,
                        sfID
                    ));
                }

                contact.Id = response.Id;
            }

            catch (Exception)
            {
                return null; // prévoir à implémenter un sytème de log
            }

            return contact;
        }

        /// <summary>
        ///     Permet de supprimer un contact dans SalesForce
        /// </summary>
        public async Task<bool> DeleteContactPE(string sfId, ContactParentEnfantDTO contact)
        {
            bool response = false;
            try
            {
                string contactId = contact.Id;
                response = await _salesForceService.DeleteContactAsync(contactId);

                if (!response)
                {
                    throw new Exception(String.Format(
                        "Could not delete the contact {0} {1} for family account id: {2}",
                        contact.Prenom,
                        contact.Nom,
                        sfId
                    ));
                }
            }

            catch (Exception)
            {
                return false; // prévoir un système de log dans le futur
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfID"></param>
        /// <returns></returns>
        public async Task<List<ContactParentEnfantDTO>> GetContactsBySFId(string sfID)
        {
            List<ContactParentEnfantDTO> contacts = new List<ContactParentEnfantDTO>();
            List<ContactParentEnfantSalesForceDTO> sfContacts = await _salesForceService.GetContactsPEFromSFIDAsync(sfID);

            if (sfContacts != null)
            {
                foreach (var sfContact in sfContacts)
                {
                    contacts.Add(new ContactParentEnfantDTO()
                    {
                        Id = sfContact.Id,
                        Nom = sfContact.LastName,
                        Prenom = sfContact.FirstName,
                        ContactType = await _salesForceService.GetRecordTypeNameAsync(sfContact.RecordTypeId),
                        Email = sfContact.Email
                    });
                }
            }


            else
            {
                // This need to have a logger APIs to be able to catch exceptions and being capable to troubleshoot.
            }
            

            return contacts;
        }

        public async Task<AccountFamilleDTO> GetAccountFamilleBySfId(string sfID)
        {
            string[] fields = new string[]
            {
                "Name"
            };

            try
            {
                var queryAccountSF = await _salesForceService.GetSfAccount(sfID, fields);
                var accountSF = queryAccountSF.Records.FirstOrDefault();
                if (accountSF == null)
                {
                    return null;
                }

                string[] name = accountSF.Name.Split(' ');

                var accountFamille = new AccountFamilleDTO
                {
                    Nom = name[1],
                    Prenom = name[0]
                };

                return accountFamille;
            }

            catch (Exception)
            {
                return null;
            }
        }

        public async Task<AccountFamilleDTO> GetAccountFamilleInformationDetailsBySfId(string sfID)
        {
            try
            {
                var sfAccount = await _salesForceService.GetAllAccountFamilleInformationAsync(sfID);

                var sfNbEnfantsACharge = sfAccount.LPCR_NombreEnfantsCharge__c;
                int nbEnfantsACharge = 0;
                if (sfNbEnfantsACharge != null)
                {
                    nbEnfantsACharge = Int32.Parse(
                        sfNbEnfantsACharge,
                        NumberStyles.Float | NumberStyles.AllowThousands,
                        new CultureInfo("en-US")
                    );
                }

                var account = new AccountFamilleDTO
                {
                    Adresse = sfAccount.ShippingAddress,
                    Tel = sfAccount.Phone,
                    NumeroAllocataire = sfAccount.LPCR_NumeroAllocataire__c,
                    NbEnfantsACharge = nbEnfantsACharge,
                    EnfantHandicape = sfAccount.LPCR_Handicap__c,
                    Garde = sfAccount.LPCR_Garde__c,
                    Parents = new List<ContactParentDTO>(),
                    Enfants = new List<ContactEnfantDTO>()
                };


                if (sfAccount.Contacts != null)
                {
                    foreach (var contact in sfAccount.Contacts.Records)
                    {
                        switch (contact.RecordType.Name)
                        {
                            case "Parent":
                                var parent = new ContactParentDTO
                                {
                                    Id = contact.Id,
                                    Nom = contact.LastName,
                                    Prenom = contact.FirstName,
                                    Email = contact.Email,
                                    Tel = contact.Phone,
                                    TelPortable = contact.MobilePhone,
                                    AutoriteParentale = contact.LPCR_AutoriteParentale__c
                                };
                                (account.Parents as List<ContactParentDTO>).Add(parent);
                                break;

                            case "Enfant":
                                var enfant = new ContactEnfantDTO
                                {
                                    Id = contact.Id,
                                    Nom = contact.LastName,
                                    Prenom = contact.FirstName,
                                    DateDeNaissance = contact.Birthdate
                                };
                                (account.Enfants as List<ContactEnfantDTO>).Add(enfant);
                                break;

                            default:
                                throw new Exception("type de contact inconnu");
                        }
                    }
                }

                return account;
            }

            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateAccountFamilyInformationDetails(string sfId, AccountFamilleDTO account)
        {
            try
            {
                var success = await _salesForceService.UpdateAccountFamilyInformationDetails(sfId, account);
                return success;
            }

            catch (Exception)
            {
                return false;
            }
        }
    }
}
