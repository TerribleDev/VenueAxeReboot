Feature: Multi-Tenant Data Isolation and Security Boundary
  As a SaaS platform architect
  I want strict tenant isolation enforced at the database and repository layers
  So that tenant data is never exposed or modified across organizational boundaries

  Scenario: Queries under Tenant A context never leak Tenant B records
    Given two distinct tenants "Tenant-Alpha" and "Tenant-Beta" exist
    And "Tenant-Alpha" operates a lane named "Alpha Lane"
    And "Tenant-Beta" operates a lane named "Beta Lane"
    When an operator queries lanes under "Tenant-Alpha" context
    Then only "Alpha Lane" should be returned
    And "Beta Lane" should not be visible

  Scenario: Creating an entity under active tenant context auto-assigns Tenant ID
    Given an authenticated operator for tenant "Tenant-Alpha"
    When the operator creates a new lane named "New Arena Lane"
    Then the lane should automatically be assigned the tenant ID of "Tenant-Alpha"
