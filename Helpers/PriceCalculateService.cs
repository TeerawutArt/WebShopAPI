using System;

namespace WebShoppingAPI.Helpers;

public class PriceCalculateService
{
    public double DiscountPrice(double price, double rate, bool isPercent, double maxDiscount = int.MaxValue)
    {
        double newPrice;
        if (isPercent)
        {
            double discountPrice = price - (price * rate / 100);
            //maxDiscount จะเป็นกรณีที่มีการใช้คูปองส่วนลด 
            newPrice = discountPrice > maxDiscount ? newPrice = maxDiscount : newPrice = discountPrice; //(ternary operator)
        }
        else
        {
            //ลดราคามันตรงๆนี่หละ
            double discountPrice = price - rate;
            newPrice = discountPrice;
        }
        return newPrice;
    }

}