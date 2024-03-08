using log4net.Repository;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace log4net.Tests
{
  [TestFixture]
  public class Signing
  {
    [Test]
    public void AssemblyShouldBeSigned()
    {
      // Arrange
      var asm = typeof(LoggerRepositorySkeleton).Assembly;
      // Act
      var result = asm.GetName().GetPublicKey();
      // Assert
      Expect(result)
          .Not.To.Be.Empty();
    }
  }
}