﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OData.Edm;
using Newtonsoft.Json.Linq;
using OdataToEntity.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdataToEntity.Test.AspMvcServer.Controllers
{
    [Route("api/[controller]")]
    public sealed class OrdersController
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrdersController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpDelete]
        public void Delete(OeDataContext dataContext, Model.Order order)
        {
            dataContext.Update(order);
        }
        [HttpGet]
        public async Task<ODataResult<Model.Order>> Get()
        {
            var parser = new OeAspQueryParser(_httpContextAccessor.HttpContext);
            Model.OrderContext orderContext = parser.GetDbContext<Model.OrderContext>();
            IAsyncEnumerable<Model.Order> orders = parser.ExecuteReader<Model.Order>(orderContext.Orders.Where(o => o.Id > 0));
            List<Model.Order> orderList = await orders.OrderBy(o => o.Id).ToList();
            return parser.OData(orderList);
        }
        [HttpGet("{id}")]
        public ODataResult<Model.Order> Get(int id)
        {
            var parser = new OeAspQueryParser(_httpContextAccessor.HttpContext);
            IAsyncEnumerable<Model.Order> orders = parser.ExecuteReader<Model.Order>();
            return parser.OData(orders);
        }
        [HttpGet("{id}/Items")]
        public ODataResult<Model.OrderItem> GetItems(int id)
        {
            var parser = new OeAspQueryParser(_httpContextAccessor.HttpContext);
            IAsyncEnumerable<Model.OrderItem> orderItems = parser.ExecuteReader<Model.OrderItem>();
            return parser.OData(orderItems);
        }
        [HttpPatch]
        public void Patch(OeDataContext dataContext, IDictionary<String, Object> orderProperties)
        {
            dataContext.Update(orderProperties);
        }
        [HttpPost]
        public void Post(OeDataContext dataContext, Model.Order order)
        {
            dataContext.Update(order);
        }
        [HttpGet("WithItems(itemIds={itemIds})")]
        public ODataResult<Model.OrderItem> WithItems(String itemIds)
        {
            List<int> ids = JArray.Parse(itemIds).Select(j => j.Value<int>()).ToList();

            var edmModel = (IEdmModel)_httpContextAccessor.HttpContext.RequestServices.GetService(typeof(IEdmModel));
            Db.OeDataAdapter dataAdapter = edmModel.GetDataAdapter(typeof(Model.OrderContext));
            var orderContext = (Model.OrderContext)dataAdapter.CreateDataContext();

            List<Model.OrderItem> orderItems = orderContext.OrderItems.Where(i => ids.Contains(i.Id)).ToList();
            dataAdapter.CloseDataContext(orderContext);

            return _httpContextAccessor.HttpContext.OData(orderItems);
        }
    }
}
