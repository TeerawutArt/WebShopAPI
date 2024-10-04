using System;

namespace WebShoppingAPI.Helpers;

public class PriceCalculateService
{
    public double DiscountPrice(double price, double rate, bool isPercent, double maxDiscount = int.MaxValue)
    {
        double newPrice;
        if (isPercent)
        {
            double discountPrice = (price * rate / 100);
            //maxDiscount จะเป็นกรณีที่มีการใช้คูปองส่วนลด 
            newPrice = (discountPrice > maxDiscount && maxDiscount != 0) ? price - maxDiscount : price - discountPrice; //(ternary operator)
        }
        else
        {
            //ลดราคามันตรงๆนี่หละ
            double discountPrice = rate;
            newPrice = price - discountPrice;
        }
        return Math.Round(newPrice, 2); //เอาทศนิยม 2 ตำแหน่งพอ
    }

}
