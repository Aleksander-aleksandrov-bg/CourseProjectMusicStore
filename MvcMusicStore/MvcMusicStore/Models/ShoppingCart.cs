using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcMusicStore.Models
{
    public partial class ShoppingCart
    {
            MusicStoreEntities storeDB = new MusicStoreEntities();

            string ShoppingCartId { get; set; }

            public const string CartSessionKey = "CartId";

            public static ShoppingCart GetCart(HttpContextBase context)
            {
                var cart = new ShoppingCart();
                cart.ShoppingCartId = cart.GetCartId(context);
                return cart;
            }

            //Helper method to simplify shopping cart details
            public static ShoppingCart GetCart(Controller controller)
            {
                return GetCart(controller.HttpContext);
            }

            //ADD ITEM TO THE CART METHOD
            public void AddToCart(Album Album)
            {
                //Get the matching card and album instanced
                var cartItem = storeDB.Carts.SingleOrDefault(c => c.CartId == ShoppingCartId && c.AlbumId == Album.AlbumId);

                if (cartItem == null)
                {
                    //Create a new cart item if no cart item exists
                    cartItem = new Cart
                    {
                        AlbumId = Album.AlbumId,
                        CartId = ShoppingCartId,
                        Count = 1,
                        DateCreated = DateTime.Now

                    };
                    storeDB.Carts.Add(cartItem);
                }
                else
                {
                    //If the item already exists in the card, then add one to the quantity
                    cartItem.Count++;
                }

                //Save Changes to DB
                storeDB.SaveChanges();
           }

            //REMOVE FROM THE CART METHOD
            public int RemoveFromCart(int id)
            {
                //Get The Cart
                var cartItem = storeDB.Carts.Single( cart => cart.CartId == ShoppingCartId && cart.RecordId == id);

                int itemCount = 0;

                if (cartItem != null)
                {
                    if(cartItem.Count > 1)
                    {
                        
                        cartItem.Count--;
                        itemCount = cartItem.Count;
    
                    }
                    else
                    {
                        storeDB.Carts.Remove(cartItem);
                    }

                    //Save the changes to the database
                    storeDB.SaveChanges();
                }

                return itemCount;

            }

            //METHOD TO EMPTY THE WHOLE CARD AT A TIME
            public void EmptyCart()
            {
                var cartItems = storeDB.Carts.Where(cart => cart.CartId == ShoppingCartId).ToList();

                foreach (var cartItem in cartItems)
                {
                    storeDB.Carts.Remove(cartItem);
                }
                //Save the changes to the database
                storeDB.SaveChanges();
            }

            public List<Cart> GetCartItems()
            {
                return storeDB.Carts.Where(cart => cart.CartId == ShoppingCartId).ToList();
            }

            public int GetCount()
            {
                //Get the count of each item in the card and sum them up
                int? count = (from cartItems in storeDB.Carts 
                              where cartItems.CartId == ShoppingCartId 
                              select (int?)cartItems.Count).Sum();
                //Returnt 0 if all entries are null
                return count ?? 0;
            }

            //Multiply album price by count of that album to get the current price
            //for each of those albums in the cart
            //sum all album price totals to get the cart total
            public decimal GetTotal()
            {
                decimal? total = (from cartItems in storeDB.Carts
                                  where cartItems.CartId == ShoppingCartId
                                  select (int?)cartItems.Count * cartItems.Album.Price).Sum();
                return total ?? decimal.Zero;
            }

            public int CreateOrder(Order order)
            {
                decimal orderTotal = 0;

                var cartItems = GetCartItems();

                //Iterate over the items in the cart, adding the order details for each of them
                foreach (var item in cartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        AlbumId = item.AlbumId,
                        OrderId = order.OrderId,
                        UnitPrice = item.Album.Price,
                        Quantity = item.Count
                    };

                    //Set the order total of the shopping cart
                    orderTotal += (item.Count * item.Album.Price);

                    storeDB.OrderDetails.Add(orderDetail);
                }

                //Set the order's total to the orderTotal count
                order.Total = orderTotal;

                //Save the order
                storeDB.SaveChanges();

                //Empty the shopping cart;
                EmptyCart();

                //Returnt the orderId as the confirmation number
                return order.OrderId;
            }

            //We are using HttpcontextBase to allow access to cookies.
            public string GetCartId(HttpContextBase context)
            {
                if (context.Session[CartSessionKey] == null)
                {
                    if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
                    {
                        context.Session[CartSessionKey] = context.User.Identity.Name;
                    }
                    else
                    {
                        //Generate a new random GUID using System.Guid class

                        Guid tempCartId = Guid.NewGuid();

                        //Sedn tempCartId to client as a cookkie
                        context.Session[CartSessionKey] = tempCartId.ToString();
                    }
                }
                return context.Session[CartSessionKey].ToString();
            }

            //When a user logged in, migrate their shopping cart to be associated with their username
            public void Migratecart(string userName)
            {
                var shoppingCart = storeDB.Carts.Where(c => c.CartId == ShoppingCartId);

                foreach (Cart item in shoppingCart)
                {
                    item.CartId = userName;
                }
                storeDB.SaveChanges();
            }

    }
}