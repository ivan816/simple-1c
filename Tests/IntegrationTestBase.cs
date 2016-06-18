using LinqTo1C.Impl;

namespace LinqTo1C.Tests
{
    public abstract class IntegrationTestBase : TestBase
    {
        protected const string organizationInn = "6670403101";
        protected const string usnOrganizationInn = "6677889917";
        protected const string individualOrganizationInn = "502913867425";
        protected static GlobalContext globalContext;

        protected override void SetUp()
        {
            base.SetUp();
            if (globalContext == null)
                globalContext = Testing1CConnector.Create(@"\\host\dev\testBases\houseStark", "base");
            globalContext.BeginTransaction();
        }

        protected override void TearDown()
        {
            globalContext.RollbackTransaction();
            base.TearDown();
        }
    }
}