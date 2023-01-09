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
    [Migration("20230106210957_AddEventTracking")]
    partial class AddEventTracking
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CosmosProposalBot.Data.Model.AdminRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("GuildId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("AdminRole");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.AdminUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("GuildId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("AdminUser");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Chain", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ChainId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("CustomForGuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LinkPattern")
                        .HasColumnType("nvarchar(max)");

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

                    b.Property<decimal>("DiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

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

                    b.Property<string>("Provider")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ChainId");

                    b.ToTable("Endpoints");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Guild", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Proposal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ChainId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DepositEndTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProposalId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProposalType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("SubmitTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("VotingEndTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("VotingStartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ChainId");

                    b.ToTable("Proposals");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.TrackedEvent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Height")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime?>("HeightEstimatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("NextNotificationAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProposalId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ProposalId");

                    b.ToTable("TrackedEvents");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.TrackedEventThread", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("ThreadId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<Guid>("TrackedEventId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("TrackedEventId");

                    b.ToTable("TrackedEventThread");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.UserSubscription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ChainId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("DiscordUserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ChainId");

                    b.ToTable("UserSubscriptions");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.AdminRole", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Guild", "Guild")
                        .WithMany("AdminRoles")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.AdminUser", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Guild", "Guild")
                        .WithMany("AdminUsers")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
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

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Proposal", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Chain", "Chain")
                        .WithMany("Proposals")
                        .HasForeignKey("ChainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chain");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.TrackedEvent", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.Proposal", "Proposal")
                        .WithMany()
                        .HasForeignKey("ProposalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Proposal");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.TrackedEventThread", b =>
                {
                    b.HasOne("CosmosProposalBot.Data.Model.TrackedEvent", "TrackedEvent")
                        .WithMany("Threads")
                        .HasForeignKey("TrackedEventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TrackedEvent");
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

                    b.Navigation("Proposals");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.Guild", b =>
                {
                    b.Navigation("AdminRoles");

                    b.Navigation("AdminUsers");
                });

            modelBuilder.Entity("CosmosProposalBot.Data.Model.TrackedEvent", b =>
                {
                    b.Navigation("Threads");
                });
#pragma warning restore 612, 618
        }
    }
}
