﻿// <auto-generated />
using System;
using CosmosProposalBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CosmosProposalBot.Migrations
{
    [DbContext(typeof(CopsDbContext))]
    [Migration("20221209150818_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Chain", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Chains");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.ChannelSubscription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ChainId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DiscordChannelId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ChainId");

                    b.ToTable("ChannelSubscriptions");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Endpoint", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ChainId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ChainId");

                    b.ToTable("Endpoints");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.UserSubscription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ChainId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DiscordUserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ChainId");

                    b.ToTable("UserSubscriptions");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.ChannelSubscription", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Chain", "Chain")
                        .WithMany()
                        .HasForeignKey("ChainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chain");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Endpoint", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Chain", "Chain")
                        .WithMany("Endpoints")
                        .HasForeignKey("ChainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chain");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.UserSubscription", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Chain", "Chain")
                        .WithMany()
                        .HasForeignKey("ChainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chain");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Chain", b =>
                {
                    b.Navigation("Endpoints");
                });
#pragma warning restore 612, 618
        }
    }
}
