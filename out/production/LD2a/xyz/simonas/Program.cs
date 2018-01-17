// Roko Vaitkeviciaus IFF-4/3
// Ld2a

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lygiagretus
{
    class Program
    {
        public static int Index { get; set; }

        static void Main(string[] args)
        {
            MarketPlace market = new MarketPlace();
            List<Supplier> divisions = ReadFile();

            Product[] P1;
            Product[] P2;
            Product[] P3;
            Product[] P4;
            Product[] P5;
            Product[] P6;
            Product[] P7;
            Product[] P8;
            Product[] P9;
            Product[] P10;

            P1 = divisions[0].Products.ToArray();
            P2 = divisions[1].Products.ToArray();
            P3 = divisions[2].Products.ToArray();
            P4 = divisions[3].Products.ToArray();
            P5 = divisions[4].Products.ToArray();
            P6 = divisions[5].Products.ToArray();
            P7 = divisions[6].Products.ToArray();
            P8 = divisions[7].Products.ToArray();
            P9 = divisions[8].Products.ToArray();
            P10 = divisions[9].Products.ToArray();

            var fileName = "VaitkeviciusR_L2a_rez.txt";
            System.IO.StreamWriter file = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + fileName);
            foreach (var division in divisions)
            {
                file.WriteLine($"             *** {division.Name} ***");
                file.WriteLine($"Nr.  Pavadinimas     Kiekis          Kaina");
                for (int i = 0; i < division.Products.Count; i++)
                {
                    file.WriteLine($"{i + 1,-5}" + division.Products[i].FormattedString);
                }

            }

            List<Thread> suppliersThreads = new List<Thread>();

            Thread gija_1 = new Thread(() => market.PutProducts(P1, "1"));

            Thread gija_2 = new Thread(() => market.PutProducts(P2, "2"));

            Thread gija_3 = new Thread(() => market.PutProducts(P3, "3"));

            Thread gija_4 = new Thread(() => market.PutProducts(P4, "4"));

            Thread gija_5 = new Thread(() => market.PutProducts(P5, "5"));



            suppliersThreads.Add(gija_1);
            suppliersThreads.Add(gija_2);
            suppliersThreads.Add(gija_3);
            suppliersThreads.Add(gija_4);
            suppliersThreads.Add(gija_5);

            foreach (var thread in suppliersThreads)
            {
                thread.Start();
            }

            List<Thread> buyersThreads = new List<Thread>();


            Thread gija_6 = new Thread(() => market.GetProducts(P6, "6", 0));

            Thread gija_7 = new Thread(() => market.GetProducts(P7, "7", 1));

            Thread gija_8 = new Thread(() => market.GetProducts(P8, "8", 2));

            Thread gija_9 = new Thread(() => market.GetProducts(P9, "9", 3));

            Thread gija_10 = new Thread(() => market.GetProducts(P10, "10", 4));

            buyersThreads.Add(gija_6);
            buyersThreads.Add(gija_7);
            buyersThreads.Add(gija_8);
            buyersThreads.Add(gija_9);
            buyersThreads.Add(gija_10);

            foreach (var thread in buyersThreads)
            {
                thread.Start();
            }

            foreach (var thread in buyersThreads)
            {
                thread.Join();
            }


            foreach (var thread in suppliersThreads)
            {
                thread.Join();
            }



            file.WriteLine("--------------------------------- \n");


            foreach (var threadClass in market.Products)
            {
                if (threadClass == null)
                {
                    break;
                }
                file.WriteLine(threadClass.FormattedString);
            }

            file.WriteLine(market.Products.Count);

            file.Close();

            foreach (var product in market.Products)
            {
                Console.WriteLine(product.FormattedString);
            }
        }

        private static List<Supplier> ReadFile()
        {
            var divsions = new List<Supplier>();
            System.IO.StreamReader file =
               new System.IO.StreamReader("VaitkeviciusR_L2a_dat_1.txt");
            for (int i = 0; i < 10; i++)
            {
                var line = file.ReadLine();
                string[] supplierLine = line?.Split(' ');
                var supplier = new Supplier();
                supplier.Name = supplierLine[0];
                for (int j = 0; j < int.Parse(supplierLine[1]); j++)
                {
                    line = file.ReadLine();
                    string[] word = line?.Split(' ');
                    var product = new Product
                    {
                        Name = word[0],
                        Quantity = int.Parse(word[1]),
                        Price = double.Parse(word[2])
                    };
                    supplier.Products.Add(product);
                }
                divsions.Add(supplier);
            }

            file.Close();
            return divsions;
        }
    }

    public class Supplier
    {
        public string Name { get; set; }
        public List<Product> Products { get; set; }

        public Supplier()
        {
            Products = new List<Product>();
        }
    }

    public class ThreadClass
    {
        public Product Product { get; set; }
        public string ThreadName { get; set; }
    }

    public class Product
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }


        public string FormattedString => $"{Name,-15} {Quantity,-15} {Price,-15}";
    }

    public class MarketPlace
    {
        public List<Product> Products = new List<Product>();

        private int[] kitimai = new int[5];


        private int suppliers = 0;

        public void PutProducts(Product[] products, string n)
        {

            foreach (var product in products)
            {
                Thread.Sleep(20);
                lock (Products)
                {
                    Console.WriteLine(n);
                    Console.WriteLine("Dedu");
                    var tempProduc = Products.SingleOrDefault(p => p.Name == product.Name);
                    if (tempProduc == null)
                    {
                        var i = FindAPlace(product);
                        Products.Insert(i, product);
                    }
                    else
                    {
                        tempProduc.Quantity += product.Quantity;
                    }
                    Monitor.PulseAll(Products);
                }

            }

            suppliers++;
        }

        private int FindAPlace(Product product)
        {
            var value = 0;
            for (int i = 0; i < Products.Count; i++)
            {
                if (string.Compare(Products[i].Name, product.Name, StringComparison.Ordinal) < 0)
                {
                    value = i + 1;
                }
            }

            return value;
        }

        public int Take(Product product)
        {
            var tempProduc = Products.SingleOrDefault(p => p.Name == product.Name);
            if (tempProduc != null && tempProduc.Quantity >= product.Quantity)
            {
                tempProduc.Quantity -= product.Quantity;
                if (tempProduc.Quantity == 0)
                {
                    Products.Remove(tempProduc);

                }
                return 1;
            }
            return -1;
        }

        public void GetProducts(Product[] products, string n, int sk)
        {
            lock (Products)
            {
                while (Products.Count == 0)
                {
                    Console.WriteLine("LAUKIU");
                    Monitor.Wait(Products);
                }
            }
            while (suppliers != 5 || kitimai[sk] < products.Length)
            {
                kitimai[sk] = 0;

                {
                    foreach (var product in products)
                    {
                        Thread.Sleep(20);
                        lock (Products)
                        {
                            Console.WriteLine($"Imu: {product.Name} - {sk}");
                            int el = Take(product);
                            if (el == -1)
                                kitimai[sk]++;
                        }
                        Thread.Sleep(10);
                    }
                }
            }
        }
    }
}

