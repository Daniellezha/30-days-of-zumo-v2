﻿using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Authentication;
using backend.dotnet.DataObjects;
using backend.dotnet.Models;

namespace backend.dotnet.Controllers
{
    [Authorize]
    public class TodoItemController : TableController<TodoItem>
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            MyDbContext context = new MyDbContext();
            DomainManager = new EntityDomainManager<TodoItem>(context, Request, enableSoftDelete: true);
        }

        // GET tables/TodoItem
        public async Task<IQueryable<TodoItem>> GetAllTodoItem()
        {
            Debug.WriteLine("GET tables/TodoItem");
            var emailAddr = await GetEmailAddress();
            Debug.WriteLine($"Email Address = {emailAddr}");
            return Query().Where(item => item.UserId == emailAddr);
        }

        // GET tables/TodoItem/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public async Task<SingleResult<TodoItem>> GetTodoItem(string id)
        {
            Debug.WriteLine($"GET tables/TodoItem/{id}");
            var emailAddr = await GetEmailAddress();
            var query = Lookup(id).Queryable.Where(item => item.UserId == emailAddr);
            return new SingleResult<TodoItem>(query);
        }

        // PATCH tables/TodoItem/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public async Task<TodoItem> PatchTodoItem(string id, Delta<TodoItem> patch)
        {
            Debug.WriteLine($"PATCH tables/TodoItem/{id}");
            var item = Lookup(id).Queryable.FirstOrDefault<TodoItem>();
            if (item == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            var emailAddr = await GetEmailAddress();
            if (item.UserId != emailAddr)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
            return await UpdateAsync(id, patch);
        }

        // POST tables/TodoItem
        public async Task<IHttpActionResult> PostTodoItem(TodoItem item)
        {
            Debug.WriteLine($"POST tables/TodoItem");
            var emailAddr = await GetEmailAddress();
            Debug.WriteLine($"Email Address = {emailAddr}");
            item.UserId = emailAddr;
            Debug.WriteLine($"Item = {item}");
            try
            {
                TodoItem current = await InsertAsync(item);
                Debug.WriteLine($"Updated Item = {current}");
                return CreatedAtRoute("Tables", new { id = current.Id }, current);
            }
            catch (HttpResponseException ex)
            {
                Debug.WriteLine($"Exception: {ex}");
                Debug.WriteLine($"Response: {ex.Response}");
                string content = await ex.Response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response Content: {content}");
                throw ex;
            }
        }

        // DELETE tables/TodoItem/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public async Task DeleteTodoItem(string id)
        {
            Debug.WriteLine($"DELETE tables/TodoItem/{id}");
            var item = Lookup(id).Queryable.FirstOrDefault<TodoItem>();
            if (item == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            var emailAddr = await GetEmailAddress();
            if (item.UserId != emailAddr)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
            await DeleteAsync(id);
        }

        private string GetAzureSID()
        {
            var principal = this.User as ClaimsPrincipal;
            var sid = principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            return sid;
        }

        private async Task<string> GetEmailAddress()
        {
            var credentials = await User.GetAppServiceIdentityAsync<AzureActiveDirectoryCredentials>(Request);
            return credentials.UserClaims
                .Where(claim => claim.Type.EndsWith("/emailaddress"))
                .First<Claim>()
                .Value;
        }
    }
}