﻿// <auto-generated />
using System;
using CustomDatabase2.InvoiceCode.Sharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CustomDatabase2.InvoiceCode.Sharding.EfCoreCode.Migrations
{
    [DbContext(typeof(ShardingSingleDbContext))]
    [Migration("20230703145757_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses.CompanyTenant", b =>
                {
                    b.Property<int>("CompanyTenantId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CompanyTenantId"), 1L, 1);

                    b.Property<int>("AuthPTenantId")
                        .HasColumnType("int");

                    b.Property<string>("CompanyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("CompanyTenantId");

                    b.ToTable("Companies");
                });

            modelBuilder.Entity("CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses.Invoice", b =>
                {
                    b.Property<int>("InvoiceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("InvoiceId"), 1L, 1);

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<string>("InvoiceName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("InvoiceId");

                    b.ToTable("Invoices");
                });

            modelBuilder.Entity("CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses.LineItem", b =>
                {
                    b.Property<int>("LineItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LineItemId"), 1L, 1);

                    b.Property<int>("InvoiceId")
                        .HasColumnType("int");

                    b.Property<string>("ItemName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NumberItems")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("LineItemId");

                    b.HasIndex("InvoiceId");

                    b.ToTable("LineItems");
                });

            modelBuilder.Entity("CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses.LineItem", b =>
                {
                    b.HasOne("CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses.Invoice", null)
                        .WithMany("LineItems")
                        .HasForeignKey("InvoiceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("CustomDatabase2.InvoiceCode.Sharding.EfCoreClasses.Invoice", b =>
                {
                    b.Navigation("LineItems");
                });
#pragma warning restore 612, 618
        }
    }
}
