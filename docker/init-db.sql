-- Initialize HD Platform Database
-- This script sets up the initial database structure

CREATE DATABASE hdplatform;
CREATE USER hduser WITH ENCRYPTED PASSWORD 'hdplatform123';
GRANT ALL PRIVILEGES ON DATABASE hdplatform TO hduser;

\c hdplatform;

-- Grant schema permissions
GRANT ALL ON SCHEMA public TO hduser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO hduser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO hduser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO hduser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO hduser;

-- Create tables manually (EF Core will manage schema updates)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- API Keys table
CREATE TABLE IF NOT EXISTS "ApiKeys" (
    "Id" serial PRIMARY KEY,
    "Key" varchar(100) UNIQUE NOT NULL,
    "Name" varchar(100) NOT NULL,
    "Email" varchar(255) NOT NULL,
    "Tier" varchar(50) NOT NULL DEFAULT 'free',
    "Active" boolean NOT NULL DEFAULT true,
    "MonthlyLimit" integer NOT NULL DEFAULT 50,
    "CurrentMonthUsage" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastUsed" timestamp with time zone,
    "StripeCustomerId" varchar(255),
    "MonthlyRevenue" decimal(10,2) NOT NULL DEFAULT 0
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_ApiKeys_Email" ON "ApiKeys" ("Email");
CREATE INDEX IF NOT EXISTS "IX_ApiKeys_Key" ON "ApiKeys" ("Key");

-- API Usage table  
CREATE TABLE IF NOT EXISTS "ApiUsage" (
    "Id" serial PRIMARY KEY,
    "ApiKeyId" integer NOT NULL,
    "Endpoint" varchar(50) NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "Date" date NOT NULL,
    "ResponseTimeMs" integer NOT NULL,
    "Success" boolean NOT NULL,
    "ErrorMessage" varchar(255),
    "IpAddress" varchar(45),
    "UserAgent" varchar(255),
    CONSTRAINT "FK_ApiUsage_ApiKeys_ApiKeyId" FOREIGN KEY ("ApiKeyId") REFERENCES "ApiKeys" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ApiUsage_ApiKeyId_Date" ON "ApiUsage" ("ApiKeyId", "Date");
CREATE INDEX IF NOT EXISTS "IX_ApiUsage_Date" ON "ApiUsage" ("Date");
CREATE INDEX IF NOT EXISTS "IX_ApiUsage_Endpoint" ON "ApiUsage" ("Endpoint");

-- Customers table
CREATE TABLE IF NOT EXISTS "Customers" (
    "Id" serial PRIMARY KEY,
    "Email" varchar(255) NOT NULL,
    "Name" varchar(100),
    "StripeCustomerId" varchar(50) UNIQUE NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "TotalRevenue" decimal(10,2) NOT NULL DEFAULT 0,
    "Active" boolean NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS "IX_Customers_Email" ON "Customers" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Customers_StripeCustomerId" ON "Customers" ("StripeCustomerId");

-- Subscriptions table
CREATE TABLE IF NOT EXISTS "Subscriptions" (
    "Id" serial PRIMARY KEY,
    "CustomerId" integer NOT NULL,
    "ApiKeyId" integer NOT NULL,
    "StripeSubscriptionId" varchar(50) UNIQUE NOT NULL,
    "StripePriceId" varchar(50) NOT NULL,
    "Status" varchar(50) NOT NULL,
    "PlanName" varchar(50) NOT NULL,
    "MonthlyPrice" decimal(10,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CanceledAt" timestamp with time zone,
    "CurrentPeriodStart" timestamp with time zone NOT NULL,
    "CurrentPeriodEnd" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_Subscriptions_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Subscriptions_ApiKeys_ApiKeyId" FOREIGN KEY ("ApiKeyId") REFERENCES "ApiKeys" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subscriptions_StripeSubscriptionId" ON "Subscriptions" ("StripeSubscriptionId");
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_CustomerId" ON "Subscriptions" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_ApiKeyId" ON "Subscriptions" ("ApiKeyId");

-- Sample data for testing
INSERT INTO "ApiKeys" ("Key", "Name", "Email", "Tier", "MonthlyLimit", "MonthlyRevenue") 
VALUES 
    ('hd_admin_test_key', 'Admin Test', 'admin@hdchartapi.com', 'business', 100000, 99.00),
    ('hd_free_demo_key', 'Demo User', 'demo@hdchartapi.com', 'free', 50, 0.00)
ON CONFLICT ("Key") DO NOTHING;

-- Views for Grafana dashboards
CREATE OR REPLACE VIEW daily_metrics AS
SELECT 
    DATE_TRUNC('day', "Timestamp") as date,
    COUNT(*) as total_requests,
    COUNT(*) FILTER (WHERE "Success" = true) as successful_requests,
    COUNT(*) FILTER (WHERE "Success" = false) as failed_requests,
    AVG("ResponseTimeMs") FILTER (WHERE "Success" = true) as avg_response_time,
    COUNT(DISTINCT "ApiKeyId") as unique_users
FROM "ApiUsage"
GROUP BY DATE_TRUNC('day', "Timestamp")
ORDER BY date;

CREATE OR REPLACE VIEW revenue_metrics AS
SELECT 
    DATE_TRUNC('month', "CreatedAt") as month,
    COUNT(*) as new_customers,
    SUM("MonthlyRevenue") as monthly_revenue,
    COUNT(*) FILTER (WHERE "Tier" = 'pro') as pro_customers,
    COUNT(*) FILTER (WHERE "Tier" = 'business') as business_customers,
    COUNT(*) FILTER (WHERE "Tier" = 'free') as free_customers
FROM "ApiKeys"
WHERE "Active" = true
GROUP BY DATE_TRUNC('month', "CreatedAt")
ORDER BY month;

CREATE OR REPLACE VIEW endpoint_stats AS
SELECT 
    "Endpoint",
    COUNT(*) as total_calls,
    COUNT(*) FILTER (WHERE "Success" = true) as successful_calls,
    AVG("ResponseTimeMs") FILTER (WHERE "Success" = true) as avg_response_time,
    COUNT(DISTINCT "ApiKeyId") as unique_users
FROM "ApiUsage"
WHERE "Date" >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY "Endpoint"
ORDER BY total_calls DESC;

-- Grant permissions on views
GRANT SELECT ON daily_metrics TO hduser;
GRANT SELECT ON revenue_metrics TO hduser;
GRANT SELECT ON endpoint_stats TO hduser;