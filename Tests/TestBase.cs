using System;
using NUnit.Framework;

namespace LinqTo1C.Tests
{
    [TestFixture]
    public abstract class TestBase
    {
        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
        }

        [SetUp]
        public void ActualSetUp()
        {
            try
            {
                SetUp();
            }
            catch (Exception ex)
            {
                try
                {
                    Console.WriteLine(ex);
                    TearDown();
                }
                catch (Exception e)
                {
                    Console.WriteLine("teardown exception: " + e);
                }
                throw;
            }
        }

        protected virtual void SetUp()
        {
        }

        [TearDown]
        protected virtual void TearDown()
        {
        }
    }
}