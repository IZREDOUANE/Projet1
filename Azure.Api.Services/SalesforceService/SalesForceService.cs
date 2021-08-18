using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.SalesForce;
using Microsoft.Extensions.Configuration;
using Salesforce.Common.Models.Json;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs.Preinscription;
using Azure.Api.Data.Models;

namespace Azure.Api.Services
{
    public class SalesForceService : ISalesForceService
    {
        private readonly ForceClient _sfClient;
        private readonly IConfiguration configuration;

        public SalesForceService(IConfiguration configuration)
        {
            this.configuration = configuration;
            _sfClient = Task.Run(SalesforceInitialize).Result;
        }

        ~SalesForceService()
        {
            _sfClient?.Dispose();
        }

        public async Task<ForceClient> SalesforceInitialize()
        {
            var sfCredentials = configuration.GetSection("SalesForceCredentials");

            SalesFroceCredentialsDTO cred = new SalesFroceCredentialsDTO();
            cred.ClientId = sfCredentials.GetValue<string>("ClientId");
            cred.ClientSecret = sfCredentials.GetValue<string>("ClientSecret");
            cred.EndPointUrl = sfCredentials.GetValue<string>("EndPointUrl");
            cred.Username = sfCredentials.GetValue<string>("Username");
            cred.Password = sfCredentials.GetValue<string>("Password");

            ForceClient sfClient = null;
            var authentication = new Salesforce.Common.AuthenticationClient();
            try
            {
                await authentication.UsernamePasswordAsync(
                    cred.ClientId, 
                    cred.ClientSecret, 
                    cred.Username, 
                    cred.Password, 
                    cred.EndPointUrl
                );

                sfClient = new ForceClient(
                    authentication.InstanceUrl, 
                    authentication.AccessToken, 
                    authentication.ApiVersion
                );
            }

            catch (Exception)
            {
                sfClient?.Dispose();
                sfClient = null;
            }

            finally
            {
                authentication?.Dispose();
            }

            

            return sfClient;
        }

        /// <summary>
        /// Test existance d'un Compte Dans SF
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<bool> IsContactExistAsync(string email)
        {
            bool IsExist = false;
            if (String.IsNullOrEmpty(email))
                return false;
            try
            {
                
                var result = await _sfClient.QueryAsync<dynamic>(String.Format("select Id From Account where Email__c  = '{0}' and RecordType.Name = 'Famille'", email));
                if (result != null)
                {
                    var totalSize = result.TotalSize;
                    if (totalSize > 0)
                        IsExist = true;
                }
            }

            catch (Exception)
            {
                return false;
            }
            
            return IsExist;
        }

        /// <summary>
        /// Restitution de l'identifiant du compte SF avec l'email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<string> GetAccountIdByEmailAsync(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                return null;
            }

            string results = null;
            try
            {   
                var qResult = await _sfClient.QueryAsync<dynamic>(String.Format("select Id From Account where Email__c  = '{0}' and RecordType.Name = 'Famille'", email));

                if (qResult != null && qResult.Records != null && qResult.TotalSize > 0)
                {
                    var Records = qResult.Records;
                    results = Records[0].Id;
                }
                else
                {
                    results = null;
                }
            }
            catch (Exception)
            {
                results = null;
            }

            return results;
        }
        /// <summary>
        /// Recupération du RecordTypeId pour la création du compte 
        /// </summary>
        /// <param name="RecordTypeIdName">Nom du record type</param>
        /// <param name="SobjectType">Type d'object (compte, contact, etc...)</param>
        /// <returns>l'ID du record type </returns>
        public async Task<string> GetRecordTypeId(string RecordTypeIdName, string SobjectType)
        {
            if (String.IsNullOrEmpty(RecordTypeIdName))
            {
                return null;
            }

            string RecordTypeID = null;
            try
            {
                
                var qResult = await _sfClient.QueryAsync<dynamic>(String.Format("SELECT Id FROM RecordType WHERE Name = '{0}' AND SobjectType='{1}'", RecordTypeIdName, SobjectType));
                if (qResult != null && qResult.Records != null && qResult.TotalSize > 0)
                {
                    var Records = qResult.Records;
                    RecordTypeID = Records[0].Id;
                }

            }
            catch (Exception)
            {
                RecordTypeID = null;
            }

            return RecordTypeID;
        }

        /// <summary>
        /// Récupère le nom d'un record type en utilisant l'id de ce dernier
        /// </summary>
        /// <param name="Id">l'Id du record type</param>
        /// <returns>Le nom du recordType</returns>
        public async Task<string> GetRecordTypeNameAsync(string Id)
        {
            string query = String.Format(
                "SELECT Name FROM RecordType WHERE Id = '{0}'",
                Id
            );

            string recordTypeName = String.Empty;
            try
            {
                
                var result = await _sfClient.QueryAsync<dynamic>(query);

                if (result.Records.Count > 0)
                {
                    var recordType = result.Records;
                    recordTypeName = recordType[0].Name;
                }
            }

            catch (Exception)
            {
                recordTypeName = String.Empty;
            }

            return recordTypeName;
        }

        /// <summary>
        /// Création du Compte Famille sur SF
        /// </summary>
        /// <param name="account"></param>
        /// <returns>Une réponse de succes de l'opération</returns>
        public async Task<SuccessResponse> CreateAccountFamilleAsync(AccountFamilleDTO account)
        {
            string sfId = String.Empty;
            SuccessResponse response;
            try
            {
                // création d'un compte Famille

                // Donnée en entrée pour la création du Compte Famille SF
                string NomCompteFamille = String.Format("{0} {1}", "Famille", account.Nom);
                string _RecordTypeId = await GetRecordTypeId("Famille", "Account");

                
                var accountFamille = new AccountSalesForceDTO { Email__c = account.Login, Name = NomCompteFamille, RecordTypeId = _RecordTypeId };
                response = await _sfClient.CreateAsync("Account", accountFamille);

                //Récupération de l'id du nouveau compte famille
                sfId = response.Id;
            }

            catch (Exception)
            {
                response = null;
            }

            foreach (var contact in account.Contacts)
            {
                var contactResponse = this.CreateContactPEAsync(sfId, contact);
                if (contactResponse == null)
                {
                    return null; // prévoir un système de log dans le futur
                }
            }

            return response;
        }

        /// <summary>
        /// Permet de créer des contacts liés à un compte
        /// </summary>
        /// <param name="sfId">l'id du compte</param>
        /// <param name="newContacts">Les contacts qui être créés</param>
        /// <returns>Une réponse de succès</returns>
        public async Task<SuccessResponse> CreateContactPEAsync(string sfId, ContactParentEnfantDTO newContact)
        {
            SuccessResponse response = null;
            try
            {
                // Données en entrée pour la création du contact SF
                string _RecordTypeId = await GetRecordTypeId(newContact.ContactType, "Contact");
                var contactFamille = new ContactParentEnfantSalesForceDTO
                {
                    Email = newContact.Email,
                    FirstName = newContact.Prenom,
                    LastName = newContact.Nom,
                    RecordTypeId = _RecordTypeId,
                    AccountId = sfId
                };

                response = await _sfClient.CreateAsync("Contact", contactFamille);
            }

            catch (Exception)
            {
                response = null; //On doit implémenter un système de log dans le futur
            }

            return response;
        }

        /// <summary>
        /// Permet de supprimer le contact en utilisant l'ID
        /// </summary>
        /// <param name="contactID">l'ID du contact</param>
        /// <returns>une réponse de succès</returns>
        public async Task<bool> DeleteContactAsync(string contactID)
        {
            bool response = false;
            try
            {
                response = await _sfClient.DeleteAsync("Contact", contactID);
            }

            catch (Exception)
            {
                response = false; // prévoir un système  de log dans le futur
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<string> GetCollaborateurSfIdFromEmailAsync(string email)
        {
            string sfId = null;
            try
            {
                

                string recordTypeId = await this.GetRecordTypeId("Collaborateur", "Contact");

                string query = String.Format(
                    "SELECT Id FROM Contact WHERE email='{0}' And RecordTypeId='{1}'",
                    email,
                    recordTypeId
                );

                var result = await _sfClient.QueryAsync<dynamic>(query);
                if (result != null && result.Records != null && result.TotalSize > 0)
                {
                    var Records = result.Records;
                    sfId = Records[0].Id;
                }
            }

            catch (Exception)
            {
                sfId = null;
            }

            return sfId;
        }

        /// <summary>
        /// Permet de récupérer la liste des contacts (Enfant et Parent) d'un compte famille
        /// </summary>
        /// <param name="sfID">l'id du compte famille</param>
        /// <returns>la liste des objets SF des contacts</returns>
        public async Task<List<ContactParentEnfantSalesForceDTO>> GetContactsPEFromSFIDAsync(string sfID)
        {
            // Récupération des records type id de nos contacts
            string _ParentRecordTypeId = await GetRecordTypeId("Parent", "Contact");
            string _ChildRecordTypeId = await GetRecordTypeId("Enfant", "Contact");

            //Cette liste de propriétés n'est pas complète. 
            //rajouter d'autres propriétés sous cette méthode si besoin
            //Ne pas créer une nouvelle méthode pour SFContactPEDTO pour d'autres propriétés
            //Si parent ou enfant
            List<string> properties = new List<string>
            {
                "Id",
                "FirstName",
                "LastName",
                "Email",
                "RecordTypeId"
            };

            string query = String.Format(
                "SELECT {0} FROM Contact WHERE (RecordTypeId='{1}' or RecordTypeId='{2}') AND AccountId='{3}'",
                String.Join(',', properties),
                _ParentRecordTypeId,
                _ChildRecordTypeId,
                sfID
            );

            QueryResult<ContactParentEnfantSalesForceDTO> result;
            try
            {
                
                result = await _sfClient.QueryAsync<ContactParentEnfantSalesForceDTO>(query);
            }

            catch (Exception)
            {
                result = null;
            }

            return result?.Records.ToList();
        }

        public async Task<QueryResult<AccountSalesForceDTO>> GetSfAccount(string id, params string[] fields)
        {
            QueryResult<AccountSalesForceDTO> result;
            try
            {
                var QueryFields = new StringBuilder(fields[0]);
                for (int i = 1; i < fields.Length; i++)
                {
                    QueryFields.Append($",{ fields[i] }");
                }

                result = await _sfClient.QueryAsync<AccountSalesForceDTO>(String.Format("SELECT {0} FROM Account WHERE id ='{1}'", QueryFields.ToString(), id));
            }

            catch (Exception)
            {
                result = null;
            }

            return result;
        }

        public async Task<AccountSalesForceDTO> GetAllAccountFamilleInformationAsync(string id)
        {
            List<string> foyerAttributes = new List<string>
            {
                "Phone",
                "ShippingAddress",
                "LPCR_NumeroAllocataire__c",
                "LPCR_Handicap__c",
                "LPCR_NombreEnfantsCharge__c",
                "LPCR_Garde__c"
            };

            List<string> contactsAttributes = new List<string>
            {
                // common 
                "Id",
                "FirstName",
                "LastName",

                // enfants
                "Birthdate",
                
                //parents
                "Email",
                "Phone",
                "MobilePhone",
                "LPCR_AutoriteParentale__c"
            };

            string foyerAttributesString = String.Join(',', foyerAttributes);
            string contactsAttributesString = String.Join(',', contactsAttributes);

            string selectQuery = @$"
                SELECT 
                    { foyerAttributesString },
                    ( SELECT
                        { contactsAttributesString },
                        c.RecordType.Name
                      FROM a.Contacts c 
                      WHERE c.RecordType.Name IN ('Parent', 'Enfant')
                    )
                FROM Account a 
                WHERE Id = '{id}'
            ";

            AccountSalesForceDTO account; 
            try
            {
                QueryResult<AccountSalesForceDTO> results;
                results = await _sfClient.QueryAsync<AccountSalesForceDTO>(selectQuery);
                account = results.Records.FirstOrDefault();

                if (account == null)
                {
                    account = new AccountSalesForceDTO();
                }
            }

            catch (Exception)
            {
                account = null;
            }

            return account;
        }


        public async Task<bool> UpdateAccountEntrepriseDetails(string sfId, AccountEntrepriseDTO account)
        {
            try
            {
                var accountAttrsToUpdate = new
                {
                    Phone = account.Phone,
                    Name = account.NomComplet
                };

                var accountUpdateResp = await _sfClient.UpdateAsync("Account", sfId, accountAttrsToUpdate);

                return accountUpdateResp.Success;
            }

            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateAccountFamilyInformationDetails(string sfId, AccountFamilleDTO account)
        {
            bool success = true;

            try
            {
                // account update 
                var accountAttrsToUpdate = new
                {
                    Phone = account.Tel

                    //ShippingStreet = account.Adresse.Street,
                    //ShippingCity = account.Adresse.City,
                    //ShippingPostalCode = account.Adresse.PostalCode
                };

                var accountUpdateResp = await _sfClient.UpdateAsync("Account", sfId, accountAttrsToUpdate);
                success &= accountUpdateResp.Success;

                // parents update
                foreach (var parent in account.Parents)
                {
                    string parentSfId = parent.Id;

                    var parentAttrsToUpdate = new
                    {
                        //FirstName = parent.Prenom,
                        //LastName = parent.Nom,
                        MobilePhone = parent.TelPortable,
                        parent.Email
                    };

                    var parentUpdateResp = await _sfClient.UpdateAsync("Contact", parentSfId, parentAttrsToUpdate);
                    success &= parentUpdateResp.Success;
                }

                // children update
                foreach (var enfant in account.Enfants)
                {
                    string enfantSfId = enfant.Id;

                    var enfantAttrsToUpdate = new
                    {
                        FirstName = enfant.Prenom,
                        LastName = enfant.Nom
                    };

                    var enfantUpdateResp = await _sfClient.UpdateAsync("Contact", enfantSfId, enfantAttrsToUpdate);
                    success &= enfantUpdateResp.Success;
                }

            }

            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountCrecheSalesForceDTO>> GetCrecheNameAndIdListAsync()
        {
            QueryResult<AccountCrecheSalesForceDTO> creches = null;
            try
            {
                

                var recordType = await this.GetRecordTypeId("Crèche", "Account");

                creches = await _sfClient.QueryAsync<AccountCrecheSalesForceDTO>(
                    String.Format(
                        "SELECT {0} FROM Account WHERE RecordTypeId='{1}'",
                        "Name, Id",
                        recordType
                    )
                );
            }

            catch (Exception)
            {
                creches = null;
            }

            return creches?.Records.ToList();
        }

        /// <summary>
        /// Get creche that belongs to the director
        /// </summary>
        /// <param name="director">the sfID of the director</param>
        /// <returns>a creche DTO object</returns>
        public async Task<List<AccountCrecheSalesForceDTO>> GetDirectorCrecheNameAndIdAsync(string director)
        {
            QueryResult<AccountCrecheSalesForceDTO> creches = null;
            try
            {
                var recordType = await this.GetRecordTypeId("Crèche", "Account");

                creches = await _sfClient.QueryAsync<AccountCrecheSalesForceDTO>(
                    String.Format(
                        "SELECT {0} FROM Account WHERE LPCR_ContactDirectrice__c='{1}' AND RecordTypeId='{2}'",
                        "Name, Id",
                        director,
                        recordType
                    )
                );
            }

            catch (Exception)
            {
                creches = null;
            }

            return creches?.Records.ToList();
        }

        public async Task<AccountContactsLpcrSalesForceDTO> GetContactsLpcrFromAccount(string accountSfId)
        {
            IEnumerable<AccountContactsLpcrSalesForceDTO> results;

            var query = GenerateContactsLpcrFromAccountQuery(accountSfId);

            try
            {
                var sfLpcrContacts = await _sfClient.QueryAsync<AccountContactsLpcrSalesForceDTO>(query);

                results = sfLpcrContacts.Records;
            }

            catch (Exception)
            {
                results = null;
            }

            return results.FirstOrDefault() ?? new AccountContactsLpcrSalesForceDTO();
        }

        private string GenerateContactsLpcrFromAccountQuery(string accountSfId)
        {
            var contactTypes = new string[]
            {
                "LPCR_ResponsableADV__r",
                "LPCR_ResponsableServiceFamille__r",
                "Owner"
            };

            var contactFields = new string[]
            {
                "FirstName",
                "LastName",
                "Email",
                "Phone",
                "MobilePhone"
            };

            var ctSize = contactTypes.Length;
            var cfSize = contactFields.Length;

            var query = new StringBuilder("SELECT ");

            for (int i = 0; i < ctSize; ++i)
            {
                for (int j = 0; j < cfSize; ++j)
                {
                    query.Append($"{ contactTypes[i] }.{ contactFields[j] }");
                    if (i != ctSize - 1 || j != cfSize - 1)
                    {
                        query.Append(",");
                    }
                }
            }

            query.Append($" FROM Account Where Id = '{ accountSfId }'");

            return query.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<ContractSalesForceDTO>> GetCrechesIdOfContractsFromUserIdAsync(string id)
        {
            List<ContractSalesForceDTO> contracts = null;
            try
            {
                
                var sfContracts = await _sfClient.QueryAsync<ContractSalesForceDTO>(
                    String.Format(
                        "SELECT LPCR_Creche__c FROM Contract WHERE AccountId='{0}' GROUP BY LPCR_Creche__c",
                        id
                    )
                );

                contracts = sfContracts.Records.ToList();
            }

            catch (Exception)
            {
                contracts = null;
            }

            return contracts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<AccountSalesForceDTO>> GetAccountIdOfContractsFromCreche(string id)
        {
            string[] fields =
            {
                "id",
                "Name"
            };

            string commaSepFields = string.Join(',', fields);

            List<AccountSalesForceDTO> accounts = null;
            try
            {
                // create a set of AcccountIds from contracts
                    string queryContracts = @$"
                    SELECT AccountId 
                    FROM Contract 
                    WHERE LPCR_Creche__c='{id}'
                ";

                var queryResultContract = await _sfClient.QueryAsync<ContractSalesForceDTO>(queryContracts);

                var contracts = queryResultContract.Records.ToList();
                var cSize = contracts.Count;

                StringBuilder accountIds = new StringBuilder();

                accountIds.Append('(');
                for (int i=0; i < cSize; ++i)
                {
                    accountIds.Append($"'{contracts[i].AccountId}'");
                    if (i != cSize-1)
                    {
                        accountIds.Append(',');
                    }
                }

                accountIds.Append(')');

                //find all the families that is in the list previously created

                string queryAccounts = @$"
                    SELECT {commaSepFields}
                    FROM Account
                    WHERE id IN {accountIds}
                ";

                var queryResultAccounts = await _sfClient.QueryAsync<AccountSalesForceDTO>(queryAccounts);
                accounts = queryResultAccounts.Records.Distinct().ToList();
            }

            catch (Exception)
            {
                accounts = null;
            }

            return accounts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<AccountCrecheSalesForceDTO>> GetAccountCrecheByUserIdAsync(string id)
        {
            List<string> attrsCreche = new List<string>
            {
                "Name",
                "Id",
                "ParentId",
                "LPCR_ContactDirectrice__c",
                "email__c",
                "LPCR_BerceauxCommercialisables__c",
                "lPCR_BerceauxDisponibles__c",
                "lPCR_TypeCreche__c",
                "ShippingAddress",
                "phone",
                "LPCR_ContactDirectrice__r.FirstName",
                "LPCR_ContactDirectrice__r.LastName",
                "LPCR_ContactDirectrice__r.Email",
                "LPCR_ContactDirectrice__r.Phone",
                "LPCR_InterlocuteurServiceFamille__r.FirstName",
                "LPCR_InterlocuteurServiceFamille__r.LastName",
                "LPCR_InterlocuteurServiceFamille__r.Email",
                "LPCR_InterlocuteurServiceFamille__r.Phone",
                "LPCR_ResponsableServiceFamille__c"
            };

            List<AccountCrecheSalesForceDTO> creches = new List<AccountCrecheSalesForceDTO>();
            try
            {
                
                string attrsCrecheStr = String.Join(",", attrsCreche);

                var crecheFromContract = await this.GetCrechesIdOfContractsFromUserIdAsync(id);
                if (crecheFromContract == null)
                {
                    throw new Exception();
                }

                HashSet<string> crecheIds = new HashSet<string>();
                foreach (var c in crecheFromContract)
                {
                    string crecheId = c.LPCR_Creche__c;
                    if (crecheIds.Contains(crecheId))
                    {
                        continue;
                    }

                    crecheIds.Add(crecheId);

                    var creche = await _sfClient.QueryAsync<AccountCrecheSalesForceDTO>(
                        String.Format(
                            "SELECT {0} FROM Account WHERE Id='{1}'",
                            attrsCrecheStr,
                            crecheId
                        )
                    );


                    creches.Add(creche.Records.FirstOrDefault());
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
                creches = null;
            }

            return creches;
        }

        // <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<AccountCrecheSalesForceDTO> GetAccountCrecheByCrecheSFId(string id)
        {
            List<string> attrsCreche = new List<string>
            {
                "Name",
                "Id",
                "ParentId",
                "LPCR_ContactDirectrice__c",
                "email__c",
                "LPCR_BerceauxCommercialisables__c",
                "lPCR_BerceauxDisponibles__c",
                "lPCR_TypeCreche__c",
                "ShippingAddress",
                "phone",
                "LPCR_ContactDirectrice__r.FirstName",
                "LPCR_ContactDirectrice__r.LastName",
                "LPCR_ContactDirectrice__r.Email",
                "LPCR_ContactDirectrice__r.Phone",
                "LPCR_InterlocuteurServiceFamille__r.FirstName",
                "LPCR_InterlocuteurServiceFamille__r.LastName",
                "LPCR_InterlocuteurServiceFamille__r.Email",
                "LPCR_InterlocuteurServiceFamille__r.Phone",
                "LPCR_ResponsableServiceFamille__c"
            };

            AccountCrecheSalesForceDTO creche = null;
            try
            {
                
                string attrsCrecheStr = String.Join(",", attrsCreche);

                var SfCreche = await _sfClient.QueryAsync<AccountCrecheSalesForceDTO>(
                        String.Format(
                            "SELECT {0} FROM Account WHERE Id='{1}'",
                            attrsCrecheStr,
                            id
                        )
                );

                creche = SfCreche.Records.FirstOrDefault();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
                creche = null;
            }

            return creche;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public async Task<string> CreateContactAsync(ContactSalesForceDTO contact)
        {
            string Id;
            try
            {
                var res = await _sfClient.CreateAsync("Contact", contact);
                Id = res.Id;
            }
            catch (Exception)
            {
                Id = null;

            }

            return Id;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<string> CreateBasicAccountAsync(AccountSalesForceDTO account)
        {
            string Id;
            try
            {
                
                var res = await _sfClient.CreateAsync("Account", account);
                Id = res.Id;
            }
            catch (Exception)
            {
                Id = null;
            }

            return Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public AccountSalesForceDTO MapRegisterFamilleDTO_to_AccountSalesForceDTO(RegisterFamilleDTO account)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="ordreParent"></param>
        /// <returns></returns>
        public ContactParentSalesForceDTO MapRegisterFamilleDTO_to_ContactSalesforceParent(RegisterFamilleDTO account, int ordreParent)
        {
            try
            {
                ContactParentSalesForceDTO contact = new ContactParentSalesForceDTO();
                if (ordreParent == 1)
                {
                    contact.FirstName = account.prenom_p1;
                    contact.Lastname = account.nom_p1;
                    contact.MailingCity = account.ville_p1;
                    contact.MailingPostalCode = account.code_postale_p1;
                    contact.MailingStreet = account.adresse_p1;
                    contact.HomePhone = account.tel_fixe_p1;
                    contact.MobilePhone = account.portable_p1;
                    contact.Email = account.email_p1;
                    contact.LPCR_NombreEnfantsCharge__c = account.nbre_enfant_charge;
                    contact.LPCR_SituationFamiliale__c = account.situation_p1;
                    contact.LPCR_AutoriteParentale__c = account.autorite_p1;
                }
                else if (ordreParent == 2)
                {
                    contact.FirstName = account.prenom_p2;
                    contact.Lastname = account.nom_p2;
                    contact.MailingCity = account.ville_p2;
                    contact.MailingPostalCode = account.code_postale_p2;
                    contact.MailingStreet = account.adresse_p2;
                    contact.HomePhone = account.tel_fixe_p2;
                    contact.MobilePhone = account.portable_p2;
                    contact.Email = account.email_p2;
                    contact.LPCR_NombreEnfantsCharge__c = account.nbre_enfant_charge_p2;
                    contact.LPCR_SituationFamiliale__c = account.situation_p2;
                    contact.LPCR_AutoriteParentale__c = account.autorite_p2;
                }

                return contact;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public Task<string> CreateAccountSalesforceAsync(RegisterFamilleDTO account)
        {
            //ContactParentSalesForceDTO parent1 = this.MapRegisterFamilleDTO_to_ContactSalesforceParent(account, 1);
            //ContactParentSalesForceDTO parent2 = this.MapRegisterFamilleDTO_to_ContactSalesforceParent(account, 2);
            //AccountSalesForceDTO accountSf = new AccountSalesForceDTO();//remplir les chmaps
            ////accountSf.id //
            //parent1.AccountId = parent2.AccountId = accountSf.id;
            //this.CreateContactAsync(parent1);
            //this.CreateContactAsync(parent2);

            //// Create a new sObject of type Contact
            //// and fill out its fields.
            //SObject
            //SObject c = new SObject();
            //System.Xml.XmlElement[] contactFields = new System.Xml.XmlElement[6];

            //// Create the contact's fields
            //System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            //contactFields[0] = doc.CreateElement("FirstName");
            //contactFields[0].InnerText = "Otto";
            //contactFields[1] = doc.CreateElement("LastName");
            //contactFields[1].InnerText = "Jespersen";
            //contactFields[2] = doc.CreateElement("Salutation");
            //contactFields[2].InnerText = "Professor";
            //contactFields[3] = doc.CreateElement("Phone");
            //contactFields[3].InnerText = "(999) 555-1234";
            //contactFields[4] = doc.CreateElement("Title");
            //contactFields[4].InnerText = "Philologist";

            //c.type = "Contact";
            //c.Any = contactFields;


            ////conta
            //var a = new { Name = "New Name", Description.t = "New Description" };
            return null;
        }

        /// <summary>
        /// Recupération des status possibles pour la pré-inscription
        /// </summary>
        /// <returns></returns>
        public async Task<List<PreinscriptionStatusDto>> GetAllStatus()
        {
            List<PreinscriptionStatusDto> results = null;
            try
            {
                const string query = @"SELECT 
                                LPCR_Statut__c Label 
                            FROM 
                                LPCR_Preinscription__c 
                            WHERE
                                LPCR_Statut__c != 'Recommandation validée' 
                            AND 
                                LPCR_Statut__c  !='Recommandation'
                            GROUP BY 
                                LPCR_Statut__c";
                
                var qResult = await _sfClient.QueryAsync<PreinscriptionStatusDto>(query);
                results = qResult.Records.ToList();
                
            }

            catch (Exception)
            {
                results = null;
            }

            return results;
        }

        public async Task<List<PreinscriptionInfoDto>> GetPreinscriptionsBy(string key, bool? filtered)
        {
            List<PreinscriptionInfoDto> results = null;
            try
            {
                var where = new StringBuilder($"LPCR_CompteFamille__c = '{key}' ");
                if (filtered.HasValue && filtered == true)
                {
                    where.Append("AND LPCR_Statut__c != 'Confirmé' AND LPCR_Statut__c != 'Inscrit' AND LPCR_Statut__c != 'Annulé'");
                }
                var query = @$"SELECT 
                                Id, 
                                Name, 
                                LPCR_Statut__c,
                                LPCR_EnfantNom__c,
                                LPCR_EnfantPrenom__c,
                                LastModifiedDate,
                                LPCR_ContactEnfant__c,
                                Tech_LienFormulaire__c,
                                LPCR_Creche__r.Name
                            FROM 
                                LPCR_Preinscription__c
                            WHERE
                                {where}";


                
                var qResult = await _sfClient.QueryAsync<PreinscriptionDto>(query);

                results = qResult?.Records.Select(r => new PreinscriptionInfoDto
                {
                    Id = r.Id,
                    RequestNumber = r.Name,
                    Statut = r.LPCR_Statut__c,
                    ChildFirstname = $"{r.LPCR_EnfantPrenom__c} {r.LPCR_EnfantNom__c?.ToUpper()}",
                    DateRequest = r.LastModifiedDate,
                    ContactEnfant = r.LPCR_ContactEnfant__c,
                    Url = r.Tech_LienFormulaire__c,
                    CrecheName = r.LPCR_Creche__r?.Name
                }).ToList();
            }

            catch (Exception)
            {
               results = null;
            }

            return results;
        }


        public async Task<SuccessResponse> UpdatePreinscriptionsById(string preinscriptionId, PreinscriptionUpdateDto statusDto)
        {
            var response = new SuccessResponse();
            try
            {
                response = await _sfClient.UpdateAsync("LPCR_Preinscription__c", preinscriptionId, statusDto);
            }
            catch (Salesforce.Common.ForceException ex)
            {
                response.Success = false;
                response.Errors = ex.Message;
            }

            catch (Exception)
            {
                response.Success = false;
            }

            return response;
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> FindAllPreinscriptionIdBy(string sfId, string accountId)
        {
            IEnumerable<KeyValuePair<string, string>> result;
            try
            {
                var queryString = $@"SELECT LPCR_ContactEnfant__c Value, Id Key
                               FROM LPCR_Preinscription__c 
                               WHERE LPCR_CompteFamille__c='{sfId}'
                               GROUP BY LPCR_ContactEnfant__c, Id";

                var query = await _sfClient.QueryAsync<KeyValuePair<string, string>>(queryString);
                result = query?.Records?.ToList();
            }

            catch (Exception)
            {
                result = null;
            }

            return result;
        }
    }

    public class Account
    {
        public const String SObjectTypeName = "Account";

        public String Id { get; set; }
        public String Name { get; set; }

    }
}
