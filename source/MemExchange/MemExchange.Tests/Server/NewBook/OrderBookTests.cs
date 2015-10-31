﻿using System.Collections.Generic;
using MemExchange.Core.SharedDto;
using MemExchange.Server.Common;
using MemExchange.Server.Outgoing;
using MemExchange.Server.Processor.Book;
using MemExchange.Server.Processor.Book.Executions;
using MemExchange.Server.Processor.Book.MatchingAlgorithms;
using MemExchange.Server.Processor.Book.Orders;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;

namespace MemExchange.Tests.Server.NewBook
{
    [TestFixture]
    public class OrderBookTests
    {
        private ILimitOrderMatchingAlgorithm limitOrderMatchingAlgorithmMock;
        private IMarketOrderMatchingAlgorithm marketOrderMatchingAlgorithmMock;
        private IOutgoingQueue outgoingQueueMock;

        [SetUp]
        public void Setup()
        {
            limitOrderMatchingAlgorithmMock = MockRepository.GenerateMock<ILimitOrderMatchingAlgorithm>();
            marketOrderMatchingAlgorithmMock = MockRepository.GenerateMock<IMarketOrderMatchingAlgorithm>();
            outgoingQueueMock = MockRepository.GenerateMock<IOutgoingQueue>();
        }

        [Test]
        public void BookShouldSetBestBidOnHigherBuyOrders()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            Assert.IsNull(orderBookBestBidAsk.BestBidPrice);

            var buyOrder1 = new LimitOrder("ABC", 10, 90.1d, WayEnum.Buy, 1);
            book.HandleLimitOrder(buyOrder1);

            Assert.AreEqual(90.1d, orderBookBestBidAsk.BestBidPrice);

            var buyOrder2 = new LimitOrder("ABC", 10, 90.2d, WayEnum.Buy, 1);
            book.HandleLimitOrder(buyOrder2);

            Assert.AreEqual(90.2d, orderBookBestBidAsk.BestBidPrice);

            var buyOrder3 = new LimitOrder("ABC", 10, 80.0d, WayEnum.Buy, 1);
            book.HandleLimitOrder(buyOrder3);

            Assert.AreEqual(90.2d, orderBookBestBidAsk.BestBidPrice);
        }

        [Test]
        public void BookShouldSetBestAskOnLowerSellOrders()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            Assert.IsNull(orderBookBestBidAsk.BestAskPrice);

            var order1 = new LimitOrder("ABC", 10, 90.1d, WayEnum.Sell, 1);
            book.HandleLimitOrder(order1);

            Assert.AreEqual(90.1d, orderBookBestBidAsk.BestAskPrice);

            var order2 = new LimitOrder("ABC", 10, 90.0d, WayEnum.Sell, 1);
            book.HandleLimitOrder(order2);

            Assert.AreEqual(90.0d, orderBookBestBidAsk.BestAskPrice);

            var order3 = new LimitOrder("ABC", 10, 99.0d, WayEnum.Sell, 1);
            book.HandleLimitOrder(order3);

            Assert.AreEqual(90.0d, orderBookBestBidAsk.BestAskPrice);
        }

        [Test]
        public void BookShouldRemoveSlotWhenOrderIsFilled()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var buyOrder = new LimitOrder("ABC", 10, 90, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder);

            Assert.IsTrue(book.PriceSlots.ContainsKey(90));
            Assert.AreEqual(buyOrder, book.PriceSlots[90].BuyOrders[0]);

            buyOrder.Modify(0);

            Assert.IsFalse(book.PriceSlots.ContainsKey(90));
        }

        [Test]
        public void BookShouldRemoveSlotWhenOrderIsDeleted()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var buyOrder = new LimitOrder("ABC", 10, 90, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder);

            Assert.IsTrue(book.PriceSlots.ContainsKey(90));
            Assert.AreEqual(buyOrder, book.PriceSlots[90].BuyOrders[0]);

            buyOrder.Delete();

            Assert.IsFalse(book.PriceSlots.ContainsKey(90));
        }

        [Test]
        public void BookShouldSetBestBidAndNullAskOnOneBuyOrder()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var buyOrder = new LimitOrder("ABC", 10, 90, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder);

            Assert.AreEqual(90, orderBookBestBidAsk.BestBidPrice);
            Assert.IsNull(orderBookBestBidAsk.BestAskPrice);
        }

        [Test]
        public void BookShouldUpdateBestBidWhenBetterBuyPriceComesIn()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var buyOrder1 = new LimitOrder("ABC", 10, 90, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder1);

            Assert.AreEqual(90, orderBookBestBidAsk.BestBidPrice);

            var buyOrder2 = new LimitOrder("ABC", 10, 91, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder2);

            Assert.AreEqual(91, orderBookBestBidAsk.BestBidPrice);

        }

        [Test]
        public void BookShouldSetBestAskAndNullBidOnOneSellOrder()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var sellOrder = new LimitOrder("ABC", 10, 90, WayEnum.Sell, 9);
            book.HandleLimitOrder(sellOrder);

            Assert.AreEqual(90, orderBookBestBidAsk.BestAskPrice);
            Assert.IsNull(orderBookBestBidAsk.BestBidPrice);
        }

        [Test]
        public void BookShouldUpdateBestAskWhenBetterSellPriceComesIn()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", limitOrderMatchingAlgorithmMock, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var sellOrder1 = new LimitOrder("ABC", 10, 90, WayEnum.Sell, 9);
            book.HandleLimitOrder(sellOrder1);

            Assert.AreEqual(90, orderBookBestBidAsk.BestAskPrice);

            var sellOrder2 = new LimitOrder("ABC", 10, 89, WayEnum.Sell, 9);
            book.HandleLimitOrder(sellOrder2);

            Assert.AreEqual(89, orderBookBestBidAsk.BestAskPrice);
        }

        [Test]
        public void IncomingBuyOrderShouldBeMatchedCompletelyAndNotBooked()
        {
            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var book = new OrderBook("ABC", new LimitOrderMatchingAlgorithm(new DateService()), new MarketOrderMatchingAlgorithm(new DateService()),  orderBookBestBidAsk, outgoingQueueMock);

            var sellOrder1 = new LimitOrder("ABC", 100, 90, WayEnum.Sell, 9);
            book.HandleLimitOrder(sellOrder1);

            var buyOrder1 = new LimitOrder("ABC", 10, 90, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder1);

            Assert.AreEqual(0, buyOrder1.Quantity);
            Assert.AreEqual(90, sellOrder1.Quantity);

            Assert.AreEqual(0, book.PriceSlots[90].BuyOrders.Count);
            Assert.AreEqual(1, book.PriceSlots[90].SellOrders.Count);
        }

        [Test]
        public void HigherBuyOrderShouldMatchLowerSellOrdersAndPlaceBuyOrderInBookOnRestQuantity()
        {
            var executions = new List<INewExecution>();

            var orderBookBestBidAsk = new OrderBookBestBidAsk("ABC");
            var matchAlgo = new LimitOrderMatchingAlgorithm(new DateService());
            matchAlgo.AddExecutionsHandler(executions.Add);

            var book = new OrderBook("ABC", matchAlgo, marketOrderMatchingAlgorithmMock, orderBookBestBidAsk, outgoingQueueMock);

            var sellOrder1 = new LimitOrder("ABC", 10, 90, WayEnum.Sell, 9);
            var sellOrder2 = new LimitOrder("ABC", 10, 91, WayEnum.Sell, 9);
            var sellOrder3 = new LimitOrder("ABC", 10, 92, WayEnum.Sell, 9);
            var sellOrder4 = new LimitOrder("ABC", 10, 93, WayEnum.Sell, 9);
            var sellOrder5 = new LimitOrder("ABC", 10, 94, WayEnum.Sell, 9);
            
            book.HandleLimitOrder(sellOrder1);
            book.HandleLimitOrder(sellOrder2);
            book.HandleLimitOrder(sellOrder3);
            book.HandleLimitOrder(sellOrder4);
            book.HandleLimitOrder(sellOrder5);

            Assert.AreEqual(90, orderBookBestBidAsk.BestAskPrice);
            Assert.AreEqual(10, orderBookBestBidAsk.BestAskQuantity);

            var buyOrder1 = new LimitOrder("ABC", 100, 93, WayEnum.Buy, 9);
            book.HandleLimitOrder(buyOrder1);

            Assert.AreEqual(4, executions.Count);
            Assert.AreEqual(60, buyOrder1.Quantity);

            Assert.AreEqual(94, orderBookBestBidAsk.BestAskPrice);
            Assert.AreEqual(10, orderBookBestBidAsk.BestAskQuantity);

            Assert.AreEqual(93, orderBookBestBidAsk.BestBidPrice);
            Assert.AreEqual(60, orderBookBestBidAsk.BestBidQuantity);
        }

    }
}
