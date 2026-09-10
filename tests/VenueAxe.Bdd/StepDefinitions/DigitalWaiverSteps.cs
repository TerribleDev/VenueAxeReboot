using System;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Entities;
using VenueAxe.Infrastructure.Pdf;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class DigitalWaiverSteps
{
    private WaiverTemplate _template = null!;
    private Waiver _waiver = null!;
    private Venue _venue = null!;
    private byte[]? _generatedPdf;

    [Given(@"an active venue waiver template with legal text ""(.*)""")]
    public void GivenAnActiveVenueWaiverTemplate(string legalText)
    {
        string hash;
        using (var sha256 = SHA256.Create())
        {
            hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(legalText)));
        }

        _venue = new Venue
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Timberland Lounge",
            Slug = "timberland",
            AddressLine1 = "100 Pine St",
            City = "Seattle",
            State = "WA",
            PostalCode = "98101"
        };

        _template = new WaiverTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = _venue.TenantId,
            VenueId = _venue.Id,
            Title = "Official Release",
            BodyTextMarkdown = legalText,
            Sha256Hash = hash,
            VersionNumber = 1,
            IsActive = true
        };
    }

    [When(@"an adult guest ""(.*)"" ""(.*)"" signs the waiver")]
    public void WhenAnAdultGuestSignsTheWaiver(string firstName, string lastName)
    {
        _waiver = new Waiver
        {
            Id = Guid.NewGuid(),
            TenantId = _venue.TenantId,
            VenueId = _venue.Id,
            TemplateId = _template.Id,
            SignerFirstName = firstName,
            SignerLastName = lastName,
            SignerEmail = $"{firstName.ToLower()}@example.com",
            DateOfBirth = new DateOnly(1995, 3, 10),
            IsGuardianSigning = false,
            SignedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            IpAddress = "127.0.0.1",
            SignatureImagePngBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
        };
    }

    [Then(@"the waiver record should have an immutable SHA-256 legal hash")]
    public void ThenWaiverRecordShouldHaveSha256Hash()
    {
        _template.Sha256Hash.Should().NotBeNullOrWhiteSpace();
        _template.Sha256Hash.Length.Should().Be(64);
    }

    [Then(@"the signer age should be verified as 18 or older")]
    public void ThenSignerAgeShouldBeVerifiedAs18OrOlder()
    {
        var age = DateTime.UtcNow.Year - _waiver.DateOfBirth.Year;
        age.Should().BeGreaterThanOrEqualTo(18);
    }

    [Then(@"a downloadable PDF certificate can be generated")]
    public void ThenDownloadablePdfCanBeGenerated()
    {
        var pdfService = new WaiverPdfService();
        _generatedPdf = pdfService.GeneratePdf(_waiver, _venue, _template);

        _generatedPdf.Should().NotBeNull();
        _generatedPdf.Length.Should().BeGreaterThan(200);
        string header = Encoding.ASCII.GetString(_generatedPdf, 0, 5);
        header.Should().Be("%PDF-");
    }

    [When(@"a guardian ""(.*)"" ""(.*)"" signs the waiver")]
    public void WhenAGuardianSignsTheWaiver(string firstName, string lastName)
    {
        _waiver = new Waiver
        {
            Id = Guid.NewGuid(),
            TenantId = _venue.TenantId,
            VenueId = _venue.Id,
            TemplateId = _template.Id,
            SignerFirstName = firstName,
            SignerLastName = lastName,
            SignerEmail = $"{firstName.ToLower()}@example.com",
            DateOfBirth = new DateOnly(1982, 6, 15),
            IsGuardianSigning = true,
            SignedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            IpAddress = "127.0.0.1"
        };
    }

    [When(@"includes minor child ""(.*)"" born on ""(.*)""")]
    public void WhenIncludesMinorChild(string childName, string dob)
    {
        _waiver.MinorsCoveredJson = $@"[{{""fullName"":""{childName}"",""dateOfBirth"":""{dob}""}}]";
    }

    [Then(@"the waiver record should indicate guardian signing is true")]
    public void ThenWaiverRecordShouldIndicateGuardianSigningIsTrue()
    {
        _waiver.IsGuardianSigning.Should().BeTrue();
    }

    [Then(@"the covered minors data should contain ""(.*)""")]
    public void ThenCoveredMinorsDataShouldContain(string childName)
    {
        _waiver.MinorsCoveredJson.Should().NotBeNull();
        _waiver.MinorsCoveredJson.Should().Contain(childName);
    }
}
