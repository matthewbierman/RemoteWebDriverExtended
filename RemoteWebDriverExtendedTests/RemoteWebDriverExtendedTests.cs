using Microsoft.VisualStudio.TestTools.UnitTesting;
using RemoteWebDriverExtended;
using OpenQA.Selenium;
using System.Linq;

namespace RemoteWebDriverExtended.Tests
{
    [TestClass()]
    public class RemoteWebDriverExtendedTests
    {
      

        [TestMethod()]
        public void KillAllRunningWebDriversTest1()
        {
            var result = RemoteWebDriverExtended.KillAllRunningWebDrivers();

            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void KillAllRunningWebDriversTest2()
        {
            RemoteWebDriverExtended.KillAllRunningWebDrivers();

            var chromeDriver = RemoteWebDriverExtended.GetChromeDriver();

            Assert.IsNotNull(chromeDriver);

            var fireFoxDriver = RemoteWebDriverExtended.GetFireFoxDriver();

            Assert.IsNotNull(fireFoxDriver);

            var internetExplorerDriver = RemoteWebDriverExtended.GetInternetExplorerDriver();

            Assert.IsNotNull(internetExplorerDriver);

            var killWebDriverResult = RemoteWebDriverExtended.KillAllRunningWebDrivers();

            var chromeResult = killWebDriverResult.Single(r => r.Name == "chromedriver");

            Assert.AreEqual(chromeResult.Found, 1);
            Assert.AreEqual(chromeResult.Killed, 1);
            Assert.AreEqual(chromeResult.Success, true);

            var fireFoxResult = killWebDriverResult.Single(r => r.Name == "geckodriver");

            Assert.AreEqual(fireFoxResult.Found, 1);
            Assert.AreEqual(fireFoxResult.Killed, 1);
            Assert.AreEqual(fireFoxResult.Success, true);

            var internetExplorerResult = killWebDriverResult.Single(r => r.Name == "IEDriverServer");

            Assert.AreEqual(internetExplorerResult.Found, 1);
            Assert.AreEqual(internetExplorerResult.Killed, 1);
            Assert.AreEqual(internetExplorerResult.Success, true);

        }

        [TestMethod()]
        public void GetChromeDriverTest()
        {
            var driver1 = RemoteWebDriverExtended.GetChromeDriver();

            Assert.IsNotNull(driver1);

            driver1.Navigate().GoToUrl("https://google.com");

            IWebElement q1 = driver1.FindElement(By.Name("q"));

            Assert.IsNotNull(q1);

            q1.SendKeys("Test");

            driver1 = null;

            var driver2 = RemoteWebDriverExtended.GetChromeDriver();

            IWebElement q2 = driver2.FindElement(By.Name("q"));

            Assert.IsNotNull(q2);

            Assert.AreEqual(q2.GetAttribute("value"), "Test");

        }

        [TestMethod()]
        public void GetFireFoxDriverTest()
        {
            var driver1 = RemoteWebDriverExtended.GetFireFoxDriver();

            Assert.IsNotNull(driver1);

            driver1.Navigate().GoToUrl("https://google.com");

            IWebElement q1 = driver1.FindElement(By.Name("q"));

            Assert.IsNotNull(q1);

            q1.SendKeys("Test");

            driver1 = null;

            var driver2 = RemoteWebDriverExtended.GetFireFoxDriver();

            IWebElement q2 = driver2.FindElement(By.Name("q"));

            Assert.IsNotNull(q2);

            Assert.AreEqual(q2.GetAttribute("value"), "Test");
        }

        [TestMethod()]
        public void GetInternetExplorerDriverTest()
        {
            var driver1 = RemoteWebDriverExtended.GetInternetExplorerDriver();

            Assert.IsNotNull(driver1);

            driver1.Navigate().GoToUrl("https://google.com");

            IWebElement q1 = driver1.FindElement(By.Name("q"));

            Assert.IsNotNull(q1);

            q1.SendKeys("Test");

            driver1 = null;

            var driver2 = RemoteWebDriverExtended.GetInternetExplorerDriver();

            IWebElement q2 = driver2.FindElement(By.Name("q"));

            Assert.IsNotNull(q2);

            Assert.AreEqual(q2.GetAttribute("value"), "Test");
        }
    }
}