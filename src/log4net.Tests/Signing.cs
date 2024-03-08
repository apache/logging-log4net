using log4net.Repository;
using NUnit.Framework;

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
      Assert.AreNotEqual(0, result.Length);
    }
  }
}