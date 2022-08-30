using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using BlazorShared;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly string _functionUrl;
    private readonly string _serviceBusUrl;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IOptions<BaseUrlConfiguration> urlConfiguration)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _functionUrl = urlConfiguration.Value.FunctionBase;
        _serviceBusUrl = urlConfiguration.Value.AzureBusConnectionString;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        await SendDataToBlob(order);
        await SendDataToFunction(order);
    }

    private async Task SendDataToBlob(Order order)
    {
        var httpClient = new HttpClient();
        var httpmessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_functionUrl))
        {
            Content = new StringContent(JsonSerializer.Serialize(order))
        };

        await httpClient.SendAsync(httpmessage);
    }

    private async Task SendDataToFunction(Order order)
    {
        // Create a ServiceBusClient object using the connection string to the namespace.
        await using var client = new ServiceBusClient(_serviceBusUrl);

        // Create a ServiceBusSender object by invoking the CreateSender method on the ServiceBusClient object, and specifying the queue name. 
        ServiceBusSender sender = client.CreateSender("mainqueue");

        // Create a new message to send to the queue.
        string messageContent = JsonSerializer.Serialize(order);
        var message = new ServiceBusMessage(messageContent);

        // Send the message to the queue.
        await sender.SendMessageAsync(message);
    }
}
