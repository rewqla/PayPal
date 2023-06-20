using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayPal.Api;
using PayPalPayment.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PayPalPayment.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private Payment payment;
        private IHttpContextAccessor httpContextAccessor;
        IConfiguration _configuration;
        public HomeController(ILogger<HomeController> logger, IHttpContextAccessor context, IConfiguration iconfiguration)
        {
            _logger = logger;
            httpContextAccessor = context;
            _configuration = iconfiguration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public ActionResult PaymentWithPaypal(string blogId = "", string PayerID = "", string guid = "")
        {
            var ClientID = _configuration.GetValue<string>("PayPal:Key");
            var ClientSecret = _configuration.GetValue<string>("PayPal:Secret");
            var mode = _configuration.GetValue<string>("PayPal:mode");
            APIContext apiContext = PaypalConfiguration.GetAPIContext(ClientID, ClientSecret, mode);

            try
            {
                if (string.IsNullOrEmpty(PayerID))
                {
                    string baseURI = $"{this.Request.Scheme}://{this.Request.Host}/Home/PaymentWithPayPal?";
                    guid = Convert.ToString((new Random()).Next(100000));

                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);

                    string paypalRedirectUrl = createdPayment.links.FirstOrDefault(lnk => lnk.rel.ToLower().Trim() == "approval_url")?.href;

                    if (string.IsNullOrEmpty(paypalRedirectUrl))
                    {
                        return View("PaymentFailed");
                    }

                    httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var paymentId = httpContextAccessor.HttpContext.Session.GetString("payment");
                    var executedPayment = ExecutePayment(apiContext, PayerID, paymentId as string);

                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("PaymentFailed");
                    }
                    var blogIds = executedPayment.transactions[0].item_list.items[0].sku;


                    return View("PaymentSuccess");
                }
            }
            catch (Exception)
            {
                return View("PaymentFailed");
            }
        }

        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };

            this.payment = new Payment()
            {
                id = paymentId
            };

            return this.payment.Execute(apiContext, paymentExecution);
        }
        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            var itemList = new ItemList()
            {
                items = new List<Item>()
            };

            itemList.items.Add(new Item()
            {
                name = "Item Detail",
                currency = "USD",
                price = "1.00",
                quantity = "1"
            });

            itemList.items.Add(new Item()
            {
                name = "Item Detail",
                currency = "USD",
                price = "2.50",
                quantity = "2"
            });

            var payer = new Payer()
            {
                payment_method = "paypal"
            };

            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = "6"//change to the total price of products or an error!!!
            };

            var transactionList = new List<Transaction>
            {
                new Transaction
                {
                    description = "Transaction description",
                    invoice_number = Guid.NewGuid().ToString(),
                    amount = amount,
                    item_list = itemList
                }
            };

            payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            return payment.Create(apiContext);
        }
    }
}
