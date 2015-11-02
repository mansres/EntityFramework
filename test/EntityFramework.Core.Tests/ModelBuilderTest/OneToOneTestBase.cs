// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Microsoft.Data.Entity.Tests
{
    public abstract partial class ModelBuilderTest
    {
        public abstract class OneToOneTestBase : ModelBuilderTestBase
        {
            [Fact]
            public virtual void Finds_existing_navigations_and_uses_associated_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder
                    .Entity<CustomerDetails>().HasOne(d => d.Customer).WithOne(c => c.Details)
                    .HasForeignKey<CustomerDetails>(c => c.Id);
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));
                var fk = dependentType.GetForeignKeys().Single();

                var navToPrincipal = dependentType.FindNavigation(nameof(CustomerDetails.Customer));
                var navToDependent = principalType.FindNavigation(nameof(Customer.Details));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<Customer>().HasOne(e => e.Details).WithOne(e => e.Customer);

                Assert.Equal(1, dependentType.GetForeignKeys().Count());
                Assert.Same(navToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(navToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk.PrincipalKey, principalType.GetNavigations().Single().ForeignKey.PrincipalKey);
                AssertEqual(new[] { "AlternateKey", principalKey.Properties.Single().Name, Customer.NameProperty.Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Replaces_existing_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<CustomerDetails>().HasOne(c => c.Customer).WithOne();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<Customer>().HasOne(e => e.Details).WithOne(e => e.Customer);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Finds_existing_navigation_to_dependent_and_uses_associated_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>().HasOne(c => c.Details).WithOne()
                    .HasForeignKey<CustomerDetails>(c => c.Id);
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<Customer>().HasOne(e => e.Details).WithOne(e => e.Customer);

                Assert.Equal(1, dependentType.GetForeignKeys().Count());
                Assert.Equal("Customer", dependentType.GetNavigations().Single().Name);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_shadow_FK_if_existing_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>()
                    .HasOne<CustomerDetails>()
                    .WithOne()
                    .HasForeignKey<CustomerDetails>(e => e.Id);
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder.Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne(e => e.Details)
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey.DependentToPrincipal == null);
                var newFk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey != fk);

                Assert.Same(newFk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(newFk.PrincipalToDependent, principalType.GetNavigations().Single());
                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.Add(newFk.Properties.Single());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_new_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Entity<Customer>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne(e => e.Details);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(nameof(CustomerDetails.Customer), dependentType.GetNavigations().Single().Name);
                Assert.Equal(nameof(Customer.Details), principalType.GetNavigations().Single().Name);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_removes_existing_FK_when_not_specified()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder
                    .Entity<Order>()
                    .HasOne(e => e.Details)
                    .WithOne()
                    .HasForeignKey<OrderDetails>(c => c.Id);
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                modelBuilder.Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.True(fk.IsUnique);
            }

            [Fact]
            public virtual void Creates_both_navigations_and_creates_new_FK_when_not_specified()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Entity<Order>();
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal("Order", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AnotherCustomerId", "CustomerId", principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties.Single().Name, fkProperty.Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_relationship_with_navigation_to_dependent_and_new_FK_from_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(Customer));
                var principalType = model.FindEntityType(typeof(CustomerDetails));

                modelBuilder.Entity<Customer>().HasOne(e => e.Details).WithOne();

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.True(fk.IsUnique);
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
            }

            [Fact]
            public virtual void Creates_relationship_with_navigation_to_dependent_and_new_FK_from_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<CustomerDetails>().HasOne<Customer>().WithOne(e => e.Details);

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.True(fk.IsUnique);
                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
            }

            [Fact]
            public virtual void Creates_relationship_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder.Entity<CustomerDetails>().HasOne<Customer>().WithOne();

                var fk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey.PrincipalToDependent == null);

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.Add(fk.Properties.Single());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_uses_specified_FK_even_if_found_by_convention()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Order>().HasOne(e => e.Details).WithOne(e => e.Order)
                    .HasForeignKey<OrderDetails>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal("Order", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AnotherCustomerId", "CustomerId", principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties.Single().Name, fkProperty.Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_uses_specified_FK_even_if_PK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Customer>().HasOne(e => e.Details).WithOne(e => e.Customer)
                    .HasForeignKey<CustomerDetails>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal("Customer", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey", principalKey.Properties.Single().Name, "Name" }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_uses_existing_FK_not_found_by_convention()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>().HasOne<BigMak>().WithOne()
                    .HasForeignKey<Bun>(e => e.BurgerId);
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));
                var fk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey.Properties.All(p => p.Name == "BurgerId"));
                fk.IsUnique = true;

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne(e => e.Bun).WithOne(e => e.BigMak)
                    .HasForeignKey<Bun>(e => e.BurgerId);

                Assert.Same(fk, dependentType.GetForeignKeys().Single());
                Assert.Equal("BigMak", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Bun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey", principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fk.Properties.Single().Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_specified_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>();
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));

                var fkProperty = dependentType.FindProperty("BurgerId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne(e => e.Bun).WithOne(e => e.BigMak)
                    .HasForeignKey<Bun>(e => e.BurgerId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal("BigMak", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Bun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey", principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fk.Properties.Single().Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_relationship_with_specified_FK_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>();
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));

                var fkProperty = dependentType.FindProperty("BurgerId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne(e => e.Bun).WithOne()
                    .HasForeignKey<Bun>(e => e.BurgerId);

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Equal(nameof(BigMak.Bun), principalType.GetNavigations().Single().Name);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_relationship_with_specified_FK_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>();
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));

                var fkProperty = dependentType.FindProperty(nameof(Bun.BurgerId));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne<Bun>().WithOne(e => e.BigMak)
                    .HasForeignKey<Bun>(e => e.BurgerId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal(nameof(Bun.BigMak), dependentType.GetNavigations().Single().Name);
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_relationship_with_specified_FK_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>();
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<BigMak>().HasOne<Bun>().WithOne()
                    .HasForeignKey<Bun>(e => e.BurgerId);

                var newFk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey.Properties.All(p => p.Name == "BurgerId"));

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == newFk));
                Assert.Empty(principalType.GetNavigations().Where(nav => nav.ForeignKey == newFk));
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_overrides_existing_FK_when_uniqueness_does_not_match()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>().HasOne<BigMak>().WithMany()
                    .HasForeignKey(e => e.BurgerId);
                modelBuilder.Ignore<Pickle>();

                var dependentType = (IEntityType)model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));
                var fkProperty = dependentType.FindProperty(nameof(Bun.BurgerId));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne(e => e.Bun).WithOne(e => e.BigMak)
                    .HasForeignKey<Bun>(e => e.BurgerId);

                var newFk = dependentType.GetForeignKeys().Single();

                Assert.Same(fkProperty, newFk.Properties.Single());
                Assert.True(newFk.IsUnique);
                Assert.Equal(nameof(Bun.BigMak), dependentType.GetNavigations().Single().Name);
                Assert.Equal(nameof(BigMak.Bun), principalType.GetNavigations().Single().Name);
                Assert.Same(newFk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(newFk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { nameof(BigMak.AlternateKey), principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { newFk.Properties.Single().Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Removes_existing_FK_when_specified()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder
                    .Entity<OrderDetails>().HasOne<Order>().WithOne()
                    .HasForeignKey<OrderDetails>(c => c.Id);
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));
                var existingFk = dependentType.GetForeignKeys().Single(fk => fk.Properties.All(p => p.Name == "Id"));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<OrderDetails>(e => e.Id);

                var newFk = dependentType.GetForeignKeys().Single();
                Assert.Equal(existingFk.Properties, newFk.Properties);
                Assert.Equal(existingFk.PrincipalKey.Properties, newFk.PrincipalKey.Properties);
                Assert.Equal("Order", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(newFk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(newFk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AnotherCustomerId", "CustomerId", principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { existingFk.Properties.Single().Name, "OrderId" }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<OrderDetails>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(Order));
                var principalType = model.FindEntityType(typeof(OrderDetails));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<Order>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_principal_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(Customer));
                var principalType = model.FindEntityType(typeof(CustomerDetails));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne()
                    .HasForeignKey<Customer>(e => e.Id);

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_dependent_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne()
                    .HasForeignKey<CustomerDetails>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_principal_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(Customer));
                var principalType = model.FindEntityType(typeof(CustomerDetails));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne<Customer>().WithOne(e => e.Details)
                    .HasForeignKey<Customer>(e => e.Id);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal(nameof(Customer.Details), fk.DependentToPrincipal.Name);
                Assert.Null(fk.PrincipalToDependent);
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_dependent_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne<Customer>().WithOne(e => e.Details)
                    .HasForeignKey<CustomerDetails>(e => e.Id);

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.Same(fkProperty, fk.Properties.Single());
                Assert.Equal(nameof(Customer.Details), fk.PrincipalToDependent.Name);
                Assert.Null(fk.DependentToPrincipal);
                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_dependent_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne<Customer>().WithOne()
                    .HasForeignKey<CustomerDetails>(e => e.Id);

                var existingFk = dependentType.GetNavigations().Single().ForeignKey;
                var newForeignKey = dependentType.GetForeignKeys().Single(fk => fk != existingFk);
                Assert.Same(dependentType.FindProperty(nameof(Customer.Id)), newForeignKey.Properties.Single());

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == newForeignKey));
                Assert.Empty(principalType.GetNavigations().Where(nav => nav.ForeignKey == newForeignKey));
                Assert.Same(existingFk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_specified_on_principal_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(Customer));
                var principalType = model.FindEntityType(typeof(CustomerDetails));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                var principalFk = principalType.GetForeignKeys().SingleOrDefault();
                var existingFk = dependentType.GetForeignKeys().SingleOrDefault();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne<Customer>().WithOne()
                    .HasForeignKey<Customer>(e => e.Id);

                var newForeignKey = dependentType.GetForeignKeys().Single(fk => fk != existingFk);
                Assert.Same(fkProperty, newForeignKey.Properties.Single());

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == newForeignKey));
                Assert.Empty(principalType.GetNavigations().Where(nav => nav.ForeignKey == newForeignKey));
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Same(principalFk, principalType.GetForeignKeys().SingleOrDefault());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_use_PK_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var fkProperty = dependentType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne(e => e.Details)
                    .HasForeignKey<CustomerDetails>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Equal("Customer", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey", principalKey.Properties.Single().Name, "Name" }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void OneToOne_can_have_PK_explicitly_specified()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalProperty = principalType.FindProperty(Customer.IdProperty.Name);

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<Customer>().HasOne(e => e.Details).WithOne(e => e.Customer)
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(principalProperty, fk.PrincipalKey.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_use_alternate_principal_key()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));
                var principalProperty = principalType.FindProperty("AlternateKey");
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().Where(p => !((IProperty)p).IsShadowProperty).ToList();
                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Customer>().HasOne(e => e.Details).WithOne(e => e.Customer)
                    .HasPrincipalKey<Customer>(e => e.AlternateKey);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(principalProperty, fk.PrincipalKey.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Empty(principalType.GetForeignKeys());

                Assert.Equal(2, principalType.GetKeys().Count());
                Assert.Contains(principalKey, principalType.GetKeys());
                Assert.Contains(fk.PrincipalKey, principalType.GetKeys());
                Assert.NotSame(principalKey, fk.PrincipalKey);

                Assert.Equal(1, dependentType.GetKeys().Count());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());

                expectedPrincipalProperties.Add(fk.PrincipalKey.Properties.Single());
                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.Add(fk.Properties.Single());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
            }

            [Fact]
            public virtual void Can_have_both_keys_specified_explicitly()
            {
                var modelBuilder = CreateModelBuilder();
                var model = (Model)modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");
                var principalProperty = principalType.FindProperty("OrderId");

                var principalPropertyCount = principalType.PropertyCount();
                var dependentPropertyCount = dependentType.PropertyCount();
                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Order>().HasOne(e => e.Details).WithOne(e => e.Order)
                    .HasForeignKey<OrderDetails>(e => e.OrderId)
                    .HasPrincipalKey<Order>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());
                Assert.Same(principalProperty, fk.PrincipalKey.Properties.Single());

                Assert.Equal("Order", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(principalPropertyCount, principalType.PropertyCount());
                Assert.Equal(dependentPropertyCount, dependentType.PropertyCount());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_have_both_keys_specified_explicitly_in_any_order()
            {
                var modelBuilder = CreateModelBuilder();
                var model = (Model)modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");
                var principalProperty = principalType.FindProperty("OrderId");

                var principalPropertyCount = principalType.PropertyCount();
                var dependentPropertyCount = dependentType.PropertyCount();
                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Order>().HasOne(e => e.Details).WithOne(e => e.Order)
                    .HasPrincipalKey<Order>(e => e.OrderId)
                    .HasForeignKey<OrderDetails>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());
                Assert.Same(principalProperty, fk.PrincipalKey.Properties.Single());

                Assert.Equal("Order", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(principalPropertyCount, principalType.PropertyCount());
                Assert.Equal(dependentPropertyCount, dependentType.PropertyCount());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_have_both_alternate_keys_specified_explicitly()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>();
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));

                var fkProperty = dependentType.FindProperty("BurgerId");
                var principalProperty = principalType.FindProperty("AlternateKey");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne(e => e.Bun).WithOne(e => e.BigMak)
                    .HasForeignKey<Bun>(e => e.BurgerId)
                    .HasPrincipalKey<BigMak>(e => e.AlternateKey);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());
                Assert.Same(principalProperty, fk.PrincipalKey.Properties.Single());

                Assert.Equal("BigMak", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Bun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { principalProperty.Name, principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fkProperty.Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());

                Assert.Equal(2, principalType.GetKeys().Count());
                Assert.Contains(principalKey, principalType.GetKeys());
                Assert.Contains(fk.PrincipalKey, principalType.GetKeys());
                Assert.NotSame(principalKey, fk.PrincipalKey);

                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_have_both_alternate_keys_specified_explicitly_in_any_order()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<BigMak>();
                modelBuilder.Entity<Bun>();
                modelBuilder.Ignore<Pickle>();

                var dependentType = model.FindEntityType(typeof(Bun));
                var principalType = model.FindEntityType(typeof(BigMak));

                var fkProperty = dependentType.FindProperty("BurgerId");
                var principalProperty = principalType.FindProperty("AlternateKey");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<BigMak>().HasOne(e => e.Bun).WithOne(e => e.BigMak)
                    .HasPrincipalKey<BigMak>(e => e.AlternateKey)
                    .HasForeignKey<Bun>(e => e.BurgerId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());
                Assert.Same(principalProperty, fk.PrincipalKey.Properties.Single());

                Assert.Equal("BigMak", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Bun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey", principalKey.Properties.Single().Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fkProperty.Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());

                Assert.Equal(2, principalType.GetKeys().Count());
                Assert.Contains(principalKey, principalType.GetKeys());
                Assert.Contains(fk.PrincipalKey, principalType.GetKeys());
                Assert.NotSame(principalKey, fk.PrincipalKey);

                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Does_not_use_existing_FK_when_principal_key_specified()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>()
                    .HasOne<Order>().WithOne()
                    .HasForeignKey<OrderDetails>(e => e.Id);
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));
                var existingFk = dependentType.GetForeignKeys().Single(fk => fk.Properties.All(p => p.Name == "Id"));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<Order>(e => e.OrderId);

                var newFk = dependentType.GetForeignKeys().Single(fk => fk != existingFk);
                Assert.NotEqual(existingFk.Properties, newFk.Properties);
                Assert.Equal("Order", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Details", principalType.GetNavigations().Single().Name);
                Assert.Same(newFk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(newFk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var keyProperty = principalType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<Order>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(keyProperty, fk.PrincipalKey.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                var dependentType = model.FindEntityType(typeof(Order));
                var principalType = model.FindEntityType(typeof(OrderDetails));

                var keyProperty = principalType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<OrderDetails>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(keyProperty, fk.PrincipalKey.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.Add(fk.Properties.Single());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(fk.PrincipalKey, principalType.GetKeys().Single(k => k != principalKey));
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_principal_and_foreign_key_specified_on_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<OrderDetails>(e => e.OrderId)
                    .HasPrincipalKey<Order>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_principal_and_foreign_key_specified_on_dependent_in_reverse_order()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                var dependentType = model.FindEntityType(typeof(OrderDetails));
                var principalType = model.FindEntityType(typeof(Order));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<Order>(e => e.OrderId)
                    .HasForeignKey<OrderDetails>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_FK_when_principal_and_foreign_key_specified_on_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                var dependentType = model.FindEntityType(typeof(Order));
                var principalType = model.FindEntityType(typeof(OrderDetails));

                var fkProperty = dependentType.FindProperty("OrderId");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<Order>(e => e.OrderId)
                    .HasPrincipalKey<OrderDetails>(e => e.OrderId);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty, fk.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                Assert.Equal(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(fk.PrincipalKey, principalType.GetKeys().Single(k => k != principalKey));
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Principal_and_dependent_cannot_be_flipped_twice()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>()
                    .HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<OrderDetails>(e => e.Id);
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                Assert.Equal(CoreStrings.RelationshipCannotBeInverted,
                    Assert.Throws<InvalidOperationException>(() => modelBuilder
                        .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                        .HasForeignKey<OrderDetails>(e => e.OrderId)
                        .HasPrincipalKey<OrderDetails>(e => e.OrderId)).Message);
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_twice_separetely()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>()
                    .HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<OrderDetails>(e => e.Id);
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<OrderDetails>(e => e.OrderId);

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<OrderDetails>(e => e.OrderId);

                var dependentType = model.FindEntityType(typeof(Order));
                var principalType = model.FindEntityType(typeof(OrderDetails));
                var fk = dependentType.GetForeignKeys().Single();

                Assert.Same(principalType.FindProperty(nameof(OrderDetails.OrderId)), fk.PrincipalKey.Properties.Single());
                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(fk.PrincipalKey, principalType.GetKeys().Single(k => !k.IsPrimaryKey()));
                Assert.Empty(dependentType.GetKeys().Where(k => !k.IsPrimaryKey()));
            }

            [Fact]
            public virtual void Principal_and_dependent_cannot_be_flipped_twice_in_reverse_order()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>()
                    .HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<Order>(e => e.OrderId);
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                Assert.Equal(CoreStrings.RelationshipCannotBeInverted,
                    Assert.Throws<InvalidOperationException>(() => modelBuilder
                        .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                        .HasPrincipalKey<OrderDetails>(e => e.OrderId)
                        .HasForeignKey<OrderDetails>(e => e.OrderId)).Message);
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_twice_in_reverse_order_separetely()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Order>();
                modelBuilder.Entity<OrderDetails>()
                    .HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<Order>(e => e.OrderId);
                modelBuilder.Ignore<Customer>();
                modelBuilder.Ignore<CustomerDetails>();

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasPrincipalKey<Order>(e => e.OrderId);

                modelBuilder
                    .Entity<OrderDetails>().HasOne(e => e.Order).WithOne(e => e.Details)
                    .HasForeignKey<Order>(e => e.OrderId);

                var dependentType = model.FindEntityType(typeof(Order));
                var principalType = model.FindEntityType(typeof(OrderDetails));
                var fk = dependentType.GetForeignKeys().Single();

                Assert.Same(dependentType.FindProperty(nameof(Order.OrderId)), fk.Properties.Single());
                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.Same(fk.PrincipalToDependent, principalType.GetNavigations().Single());
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(fk.PrincipalKey, principalType.GetKeys().Single());
                Assert.Empty(dependentType.GetKeys().Where(k => !k.IsPrimaryKey()));
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_principal_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Customer>().HasOne(e => e.Details).WithOne()
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.Same(principalKey.Properties.Single(), fk.PrincipalKey.Properties.Single());

                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Equal(nameof(Customer.Details), fk.PrincipalToDependent.Name);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_dependent_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne()
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(principalKey.Properties.Single(), fk.PrincipalKey.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_principal_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Customer>().HasOne<CustomerDetails>().WithOne(e => e.Customer)
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(principalKey.Properties.Single(), fk.PrincipalKey.Properties.Single());

                Assert.Same(fk.DependentToPrincipal, dependentType.GetNavigations().Single());
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_dependent_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne<Customer>().WithOne(e => e.Details)
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.Same(principalKey.Properties.Single(), fk.PrincipalKey.Properties.Single());

                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Null(fk.DependentToPrincipal);
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_principal_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var existingFk = dependentType.GetForeignKeys().SingleOrDefault();
                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<Customer>().HasOne<CustomerDetails>().WithOne()
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey != existingFk);
                Assert.Same(principalKey.Properties.Single(), fk.PrincipalKey.Properties.Single());

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                Assert.Empty(principalType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.Add(fk.Properties.Single());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_principal_key_when_specified_on_dependent_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));

                var existingFk = dependentType.GetForeignKeys().SingleOrDefault();
                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<CustomerDetails>().HasOne<Customer>().WithOne()
                    .HasPrincipalKey<Customer>(e => e.Id);

                var fk = dependentType.GetForeignKeys().Single(foreignKey => foreignKey != existingFk);
                Assert.Same(principalKey.Properties.Single(), fk.PrincipalKey.Properties.Single());

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                Assert.Empty(principalType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                Assert.Equal(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.Add(fk.Properties.Single());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_uses_existing_composite_FK()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                var dependentType = model.FindEntityType(typeof(ToastedBun));
                modelBuilder.Entity<ToastedBun>().HasOne<Whoopper>().WithOne()
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var principalType = model.FindEntityType(typeof(Whoopper));
                Assert.Equal(2, dependentType.GetForeignKeys().Count());

                var principalKey = principalType.FindPrimaryKey();
                var dependentKey = dependentType.FindPrimaryKey();

                modelBuilder
                    .Entity<Whoopper>().HasOne(e => e.ToastedBun).WithOne(e => e.Whoopper)
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("ToastedBun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fk.Properties[0].Name, fk.Properties[1].Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_both_navigations_and_creates_composite_FK_specified()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<ToastedBun>();
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var dependentType = model.FindEntityType(typeof(ToastedBun));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty("BurgerId1");
                var fkProperty2 = dependentType.FindProperty("BurgerId2");

                var principalKey = principalType.FindPrimaryKey();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Whoopper>().HasOne(e => e.ToastedBun).WithOne(e => e.Whoopper)
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("ToastedBun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fkProperty1.Name, fkProperty2.Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_use_alternate_composite_key()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>(b => b.HasKey(c => new { c.Id1, c.Id2 }));
                modelBuilder.Entity<ToastedBun>();
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var dependentType = model.FindEntityType(typeof(ToastedBun));
                var principalType = model.FindEntityType(typeof(Whoopper));
                var principalProperty1 = principalType.FindProperty("AlternateKey1");
                var principalProperty2 = principalType.FindProperty("AlternateKey2");

                var fkProperty1 = dependentType.FindProperty("BurgerId1");
                var fkProperty2 = dependentType.FindProperty("BurgerId2");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Whoopper>().HasOne(e => e.ToastedBun).WithOne(e => e.Whoopper)
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 })
                    .HasPrincipalKey<Whoopper>(e => new { e.AlternateKey1, e.AlternateKey2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);
                Assert.Same(principalProperty1, fk.PrincipalKey.Properties[0]);
                Assert.Same(principalProperty2, fk.PrincipalKey.Properties[1]);

                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("ToastedBun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fkProperty1.Name, fkProperty2.Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());

                Assert.Equal(2, principalType.GetKeys().Count());
                Assert.Contains(principalKey, principalType.GetKeys());
                Assert.Contains(fk.PrincipalKey, principalType.GetKeys());
                Assert.NotSame(principalKey, fk.PrincipalKey);

                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_use_alternate_composite_key_in_any_order()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>(b => b.HasKey(c => new { c.Id1, c.Id2 }));
                modelBuilder.Entity<ToastedBun>();
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var dependentType = model.FindEntityType(typeof(ToastedBun));
                var principalType = model.FindEntityType(typeof(Whoopper));
                var principalProperty1 = principalType.FindProperty("AlternateKey1");
                var principalProperty2 = principalType.FindProperty("AlternateKey2");

                var fkProperty1 = dependentType.FindProperty("BurgerId1");
                var fkProperty2 = dependentType.FindProperty("BurgerId2");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Whoopper>().HasOne(e => e.ToastedBun).WithOne(e => e.Whoopper)
                    .HasPrincipalKey<Whoopper>(e => new { e.AlternateKey1, e.AlternateKey2 })
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);
                Assert.Same(principalProperty1, fk.PrincipalKey.Properties[0]);
                Assert.Same(principalProperty2, fk.PrincipalKey.Properties[1]);

                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("ToastedBun", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fkProperty1.Name, fkProperty2.Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());

                Assert.Equal(2, principalType.GetKeys().Count());
                Assert.Contains(principalKey, principalType.GetKeys());
                Assert.Contains(fk.PrincipalKey, principalType.GetKeys());
                Assert.NotSame(principalKey, fk.PrincipalKey);

                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Uses_composite_PK_for_FK_by_convention()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<Moostard>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<ToastedBun>();

                var dependentType = model.FindEntityType(typeof(Moostard));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty("Id1");
                var fkProperty2 = dependentType.FindProperty("Id2");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.FindPrimaryKey();

                modelBuilder
                    .Entity<Moostard>().HasOne(e => e.Whoopper).WithOne(e => e.Moostard)
                    .HasForeignKey<Moostard>(e => new { e.Id1, e.Id2 });

                var fk = dependentType.GetForeignKeys().Single();

                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Moostard", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties[0].Name, dependentKey.Properties[1].Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_and_composite_PK_is_still_used_by_convention()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<Moostard>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<ToastedBun>();

                var dependentType = model.FindEntityType(typeof(Moostard));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty("Id1");
                var fkProperty2 = dependentType.FindProperty("Id2");

                var principalKey = principalType.FindPrimaryKey();
                var dependentKey = dependentType.FindPrimaryKey();

                modelBuilder
                    .Entity<Moostard>().HasOne(e => e.Whoopper).WithOne(e => e.Moostard)
                    .HasForeignKey<Moostard>(e => new { e.Id1, e.Id2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Moostard", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties[0].Name, dependentKey.Properties[1].Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_using_principal_and_composite_PK_is_still_used_by_convention()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<Moostard>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<ToastedBun>();

                var dependentType = model.FindEntityType(typeof(Moostard));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty("Id1");
                var fkProperty2 = dependentType.FindProperty("Id2");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.FindPrimaryKey();

                modelBuilder
                    .Entity<Moostard>().HasOne(e => e.Whoopper).WithOne(e => e.Moostard)
                    .HasPrincipalKey<Whoopper>(e => new { e.Id1, e.Id2 })
                    .IsRequired();

                var fk = dependentType.GetForeignKeys().Single();

                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.Equal("Whoopper", dependentType.GetNavigations().Single().Name);
                Assert.Equal("Moostard", principalType.GetNavigations().Single().Name);
                Assert.Same(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name }, principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { dependentKey.Properties[0].Name, dependentKey.Properties[1].Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_composite_FK_when_specified_on_principal_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<ToastedBun>();
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var dependentType = model.FindEntityType(typeof(ToastedBun));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty(nameof(ToastedBun.BurgerId1));
                var fkProperty2 = dependentType.FindProperty(nameof(ToastedBun.BurgerId2));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Whoopper>().HasOne(e => e.ToastedBun).WithOne()
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });

                var fk = principalType.GetNavigations().Single().ForeignKey;
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.NotSame(fk, dependentType.GetNavigations().Single().ForeignKey);
                Assert.Equal(nameof(Whoopper.ToastedBun), fk.PrincipalToDependent.Name);
                Assert.Null(fk.DependentToPrincipal);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_composite_FK_when_specified_on_principal_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<ToastedBun>();
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var dependentType = model.FindEntityType(typeof(ToastedBun));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty(nameof(ToastedBun.BurgerId1));
                var fkProperty2 = dependentType.FindProperty(nameof(ToastedBun.BurgerId2));

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Whoopper>().HasOne<ToastedBun>().WithOne(e => e.Whoopper)
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.Equal(nameof(ToastedBun.Whoopper), fk.DependentToPrincipal.Name);
                Assert.Null(fk.PrincipalToDependent);
                Assert.NotSame(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Creates_composite_FK_when_specified_on_principal_with_no_navigations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Whoopper>().HasKey(c => new { c.Id1, c.Id2 });
                modelBuilder.Entity<ToastedBun>();
                modelBuilder.Ignore<Tomato>();
                modelBuilder.Ignore<Moostard>();

                var dependentType = model.FindEntityType(typeof(ToastedBun));
                var principalType = model.FindEntityType(typeof(Whoopper));

                var fkProperty1 = dependentType.FindProperty("BurgerId1");
                var fkProperty2 = dependentType.FindProperty("BurgerId2");

                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder
                    .Entity<Whoopper>().HasOne<ToastedBun>().WithOne()
                    .HasForeignKey<ToastedBun>(e => new { e.BurgerId1, e.BurgerId2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(fkProperty1, fk.Properties[0]);
                Assert.Same(fkProperty2, fk.Properties[1]);

                Assert.Empty(dependentType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                Assert.Empty(principalType.GetNavigations().Where(nav => nav.ForeignKey == fk));
                AssertEqual(new[] { "AlternateKey1", "AlternateKey2", principalKey.Properties[0].Name, principalKey.Properties[1].Name, principalType.GetForeignKeys().Single().Properties.Single().Name },
                    principalType.GetProperties().Select(p => p.Name));
                AssertEqual(new[] { fkProperty1.Name, fkProperty2.Name, dependentKey.Properties.Single().Name }, dependentType.GetProperties().Select(p => p.Name));
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_when_self_referencing()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef1).WithOne(e => e.SelfRef2);

                var entityType = modelBuilder.Model.FindEntityType(typeof(SelfRef));
                var fk = entityType.GetForeignKeys().Single();

                var navigationToPrincipal = fk.DependentToPrincipal;
                var navigationToDependent = fk.PrincipalToDependent;

                modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef1).WithOne(e => e.SelfRef2);

                var newFk = entityType.GetForeignKeys().Single();
                Assert.Equal(fk.Properties, newFk.Properties);
                Assert.Equal(fk.PrincipalKey, newFk.PrincipalKey);
                Assert.Equal(navigationToDependent.Name, newFk.PrincipalToDependent.Name);
                Assert.Equal(navigationToPrincipal.Name, newFk.DependentToPrincipal.Name);
                Assert.True(((IForeignKey)newFk).IsRequired);

                modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef2).WithOne(e => e.SelfRef1);

                newFk = entityType.GetForeignKeys().Single();
                Assert.Equal(fk.Properties, newFk.Properties);
                Assert.Equal(fk.PrincipalKey, newFk.PrincipalKey);
                Assert.Equal(navigationToPrincipal.Name, newFk.PrincipalToDependent.Name);
                Assert.Equal(navigationToDependent.Name, newFk.DependentToPrincipal.Name);
                Assert.True(((IForeignKey)newFk).IsRequired);
            }

            [Fact]
            public virtual void Creates_self_referencing_FK_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<SelfRef>(eb =>
                    {
                        eb.HasKey(e => e.Id);
                        eb.Property(e => e.SelfRefId);
                    });

                var entityType = modelBuilder.Model.FindEntityType(typeof(SelfRef));

                modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef1).WithOne();

                var fk = ((IEntityType)entityType).FindNavigation(nameof(SelfRef.SelfRef1)).ForeignKey;
                var navigationToPrincipal = fk.DependentToPrincipal;
                var navigationToDependent = fk.PrincipalToDependent;

                Assert.NotEqual(fk.Properties, entityType.FindPrimaryKey().Properties);
                Assert.Equal(fk.PrincipalKey, entityType.FindPrimaryKey());
                Assert.Equal(null, navigationToDependent?.Name);
                Assert.Equal(nameof(SelfRef.SelfRef1), navigationToPrincipal?.Name);
                Assert.Equal(2, entityType.GetNavigations().Count());
                Assert.True(fk.IsRequired);
            }

            [Fact]
            public virtual void Creates_self_referencing_FK_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<SelfRef>(eb =>
                    {
                        eb.HasKey(e => e.Id);
                        eb.Property(e => e.SelfRefId);
                    });

                var entityType = modelBuilder.Model.FindEntityType(typeof(SelfRef));

                modelBuilder.Entity<SelfRef>().HasOne<SelfRef>().WithOne(e => e.SelfRef1);

                var fk = ((IEntityType)entityType).FindNavigation(nameof(SelfRef.SelfRef1)).ForeignKey;
                var navigationToPrincipal = fk.DependentToPrincipal;
                var navigationToDependent = fk.PrincipalToDependent;

                Assert.NotEqual(fk.Properties, entityType.FindPrimaryKey().Properties);
                Assert.Equal(fk.PrincipalKey, entityType.FindPrimaryKey());
                Assert.Equal(nameof(SelfRef.SelfRef1), navigationToDependent?.Name);
                Assert.Equal(null, navigationToPrincipal?.Name);
                Assert.Equal(2, entityType.GetNavigations().Count());
                Assert.True(fk.IsRequired);
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_when_self_referencing_with_navigation_to_principal()
            {
                var modelBuilder = CreateModelBuilder();
                var entityBuilder = modelBuilder.Entity<SelfRef>();
                entityBuilder.Ignore(nameof(SelfRef.SelfRef2));
                entityBuilder.HasOne(e => e.SelfRef1).WithOne();

                var entityType = modelBuilder.Model.FindEntityType(typeof(SelfRef));
                var fk = entityType.GetForeignKeys().Single();

                var navigationToPrincipal = fk.DependentToPrincipal;
                var navigationToDependent = fk.PrincipalToDependent;

                modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef1).WithOne();

                Assert.Same(fk, entityType.GetForeignKeys().Single());
                Assert.Equal(navigationToDependent?.Name, fk.PrincipalToDependent?.Name);
                Assert.Equal(navigationToPrincipal.Name, fk.DependentToPrincipal.Name);
                Assert.True(((IForeignKey)fk).IsRequired);

                modelBuilder.Entity<SelfRef>().HasOne<SelfRef>().WithOne(e => e.SelfRef1);

                var newFk = entityType.GetForeignKeys().Single();

                Assert.Equal(fk.Properties, newFk.Properties);
                Assert.Equal(fk.PrincipalKey, newFk.PrincipalKey);
                Assert.Equal(navigationToPrincipal.Name, newFk.PrincipalToDependent.Name);
                Assert.Equal(navigationToDependent?.Name, newFk.DependentToPrincipal?.Name);
                Assert.True(((IForeignKey)newFk).IsRequired);
            }

            [Fact]
            public virtual void Principal_and_dependent_can_be_flipped_when_self_referencing_with_navigation_to_dependent()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<SelfRef>().HasOne<SelfRef>().WithOne(e => e.SelfRef2);

                var entityType = modelBuilder.Model.FindEntityType(typeof(SelfRef));
                var fk = entityType.FindNavigation(nameof(SelfRef.SelfRef2)).ForeignKey;

                var navigationToPrincipal = fk.DependentToPrincipal;
                var navigationToDependent = fk.PrincipalToDependent;

                modelBuilder.Entity<SelfRef>().HasOne<SelfRef>().WithOne(e => e.SelfRef2);

                Assert.Same(fk, entityType.FindNavigation(nameof(SelfRef.SelfRef2)).ForeignKey);
                Assert.Equal(navigationToDependent.Name, fk.PrincipalToDependent.Name);
                Assert.Equal(navigationToPrincipal?.Name, fk.DependentToPrincipal?.Name);
                Assert.True(((IForeignKey)fk).IsUnique);

                modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef2).WithOne();

                var newFk = entityType.FindNavigation(nameof(SelfRef.SelfRef2)).ForeignKey;

                Assert.Equal(fk.Properties, newFk.Properties);
                Assert.Equal(fk.PrincipalKey, newFk.PrincipalKey);
                Assert.Equal(navigationToPrincipal?.Name, newFk.PrincipalToDependent?.Name);
                Assert.Equal(navigationToDependent.Name, newFk.DependentToPrincipal.Name);
                Assert.True(((IForeignKey)newFk).IsUnique);
            }

            [Fact]
            public virtual void Throws_on_duplicate_navigation_when_self_referencing()
            {
                var modelBuilder = CreateModelBuilder();

                Assert.Equal(CoreStrings.DuplicateNavigation("SelfRef1", typeof(SelfRef).Name, typeof(SelfRef).Name),
                    Assert.Throws<InvalidOperationException>(() =>
                        modelBuilder.Entity<SelfRef>().HasOne(e => e.SelfRef1).WithOne(e => e.SelfRef1)).Message);
            }

            [Fact]
            public virtual void Throws_if_specified_FK_types_do_not_match()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty");
                modelBuilder.Ignore<Order>();

                Assert.Equal(CoreStrings.ForeignKeyTypeMismatch("{'GuidProperty'}", typeof(CustomerDetails).FullName, "{'Id'}", typeof(Customer).FullName),
                    Assert.Throws<InvalidOperationException>(() =>
                        modelBuilder
                            .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                            .HasPrincipalKey(typeof(Customer), "Id")
                            .HasForeignKey(typeof(CustomerDetails), "GuidProperty")).Message);
            }

            [Fact]
            public virtual void Overrides_PK_if_specified_FK_types_do_not_match_separetely()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                var guidProperty = modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty").Metadata;
                modelBuilder.Ignore<Order>();

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasPrincipalKey(typeof(Customer), nameof(Customer.Id));

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasForeignKey(typeof(CustomerDetails), "GuidProperty");

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(guidProperty, fk.Properties.Single());
                Assert.Equal(typeof(Guid), fk.PrincipalKey.Properties.Single().ClrType);
            }

            [Fact]
            public virtual void Throws_if_specified_PK_types_do_not_match()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty");
                modelBuilder.Ignore<Order>();

                Assert.Equal(CoreStrings.ForeignKeyTypeMismatch("{'GuidProperty'}", typeof(CustomerDetails).FullName, "{'Id'}", typeof(Customer).FullName),
                    Assert.Throws<InvalidOperationException>(() =>
                        modelBuilder
                            .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                            .HasForeignKey(typeof(CustomerDetails), "GuidProperty")
                            .HasPrincipalKey(typeof(Customer), "Id")).Message);
            }

            [Fact]
            public virtual void Overrides_FK_if_specified_PK_types_do_not_match_separetely()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty");
                modelBuilder.Ignore<Order>();

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasForeignKey(typeof(CustomerDetails), "GuidProperty");

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasPrincipalKey(typeof(Customer), nameof(Customer.Id));

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var principalType = model.FindEntityType(typeof(Customer));
                var fk = dependentType.GetForeignKeys().Single();
                Assert.Same(principalType.FindProperty(nameof(Customer.Id)), fk.PrincipalKey.Properties.Single());
                Assert.Equal(typeof(int), fk.Properties.Single().ClrType);
            }

            [Fact]
            public virtual void Throws_if_specified_FK_count_does_not_match()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty");
                modelBuilder.Ignore<Order>();

                Assert.Equal(CoreStrings.ForeignKeyCountMismatch("{'Id', 'GuidProperty'}", typeof(CustomerDetails).FullName, "{'Id'}", typeof(Customer).FullName),
                    Assert.Throws<InvalidOperationException>(() =>
                        modelBuilder
                            .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                            .HasPrincipalKey(typeof(Customer), "Id")
                            .HasForeignKey(typeof(CustomerDetails), "Id", "GuidProperty")).Message);
            }

            [Fact]
            public virtual void Overrides_PK_if_specified_FK_count_does_not_match_separetely()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                var guidProperty = modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty").Metadata;
                modelBuilder.Ignore<Order>();

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasPrincipalKey(typeof(Customer), nameof(Customer.Id));

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasForeignKey(typeof(CustomerDetails), nameof(CustomerDetails.Id), "GuidProperty");

                var dependentType = model.FindEntityType(typeof(CustomerDetails));
                var fk = dependentType.GetForeignKeys().Single();
                AssertEqual(new[] { dependentType.FindProperty(nameof(CustomerDetails.Id)), guidProperty }, fk.Properties);
                Assert.Equal(2, fk.PrincipalKey.Properties.Count());
            }

            [Fact]
            public virtual void Throws_if_specified_PK_count_does_not_match()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty");
                modelBuilder.Ignore<Order>();

                Assert.Equal(CoreStrings.ForeignKeyCountMismatch("{'Id', 'GuidProperty'}", typeof(CustomerDetails).FullName, "{'Id'}", typeof(Customer).FullName),
                    Assert.Throws<InvalidOperationException>(() =>
                        modelBuilder
                            .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                            .HasForeignKey(typeof(CustomerDetails), "Id", "GuidProperty")
                            .HasPrincipalKey(typeof(Customer), "Id")).Message);
            }

            [Fact]
            public virtual void Overrides_FK_if_specified_PK_count_does_not_match_separetely()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();
                modelBuilder.Entity<CustomerDetails>().Property<Guid>("GuidProperty");
                modelBuilder.Ignore<Order>();

                var principalType = model.FindEntityType(typeof(Customer));

                modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasForeignKey(typeof(CustomerDetails), nameof(CustomerDetails.Id), "GuidProperty");

                var fk = modelBuilder
                    .Entity<Customer>().HasOne(c => c.Details).WithOne(d => d.Customer)
                    .HasPrincipalKey(typeof(Customer), nameof(Customer.Id)).Metadata;

                Assert.Same(principalType.FindProperty(nameof(Customer.Id)), fk.PrincipalKey.Properties.Single());
                Assert.Equal(1, fk.Properties.Count());
            }

            [Fact]
            public virtual void Overrides_existing_many_to_one_relationship()
            {
                var modelBuilder = HobNobBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Hob>().HasOne(e => e.Nob).WithMany(e => e.Hobs);

                var dependentType = model.FindEntityType(typeof(Nob));
                var principalType = model.FindEntityType(typeof(Hob));

                modelBuilder.Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob);

                var fk = dependentType.GetNavigations().Single().ForeignKey;
                Assert.Same(fk, principalType.GetNavigations().Single().ForeignKey);
                Assert.True(fk.IsUnique);
                Assert.Equal(nameof(Nob.Hob), dependentType.GetNavigations().Single().Name);
                Assert.Equal(nameof(Hob.Nob), principalType.GetNavigations().Single().Name);
            }

            [Fact]
            public virtual void Finds_and_removes_existing_one_to_many_relationship()
            {
                var modelBuilder = HobNobBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Hob>().HasMany(e => e.Nobs).WithOne(e => e.Hob);

                var dependentType = model.FindEntityType(typeof(Nob));
                var principalType = model.FindEntityType(typeof(Hob));
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();
                var principalKey = principalType.GetKeys().Single();
                var dependentKey = dependentType.GetKeys().Single();

                modelBuilder.Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.True(fk.IsUnique);
                Assert.Equal(1, dependentType.GetNavigations().Count());
                Assert.Equal(1, principalType.GetNavigations().Count());
                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
                Assert.Empty(principalType.GetForeignKeys());
                Assert.Same(principalKey, principalType.GetKeys().Single());
                Assert.Same(dependentKey, dependentType.GetKeys().Single());
                Assert.Same(principalKey, principalType.FindPrimaryKey());
                Assert.Same(dependentKey, dependentType.FindPrimaryKey());
            }

            [Fact]
            public virtual void Can_add_annotations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<CustomerDetails>();
                modelBuilder.Entity<Customer>();
                modelBuilder.Ignore<Order>();

                var dependentType = model.FindEntityType(typeof(CustomerDetails));

                var builder = modelBuilder.Entity<CustomerDetails>().HasOne(e => e.Customer).WithOne(e => e.Details);
                builder = builder.HasAnnotation("Fus", "Ro");

                var fk = dependentType.FindNavigation(nameof(CustomerDetails.Customer)).ForeignKey;
                Assert.Same(fk, builder.Metadata);
                Assert.Equal("Ro", fk["Fus"]);
            }

            [Fact]
            public virtual void Nullable_FK_are_optional_by_default()
            {
                var modelBuilder = HobNobBuilder();

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .HasForeignKey<Nob>(e => new { e.HobId1, e.HobId2 });

                var entityType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));

                Assert.False(entityType.GetForeignKeys().Single().IsRequired);
                Assert.True(entityType.FindProperty(nameof(Nob.HobId1)).IsNullable
                            || entityType.FindProperty(nameof(Nob.HobId2)).IsNullable);
            }

            [Fact]
            public virtual void Non_nullable_FK_are_required_by_default()
            {
                var modelBuilder = HobNobBuilder();

                modelBuilder
                    .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                    .HasForeignKey<Hob>(e => new { e.NobId1, e.NobId2 });

                var entityType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));

                Assert.False(entityType.FindProperty(nameof(Hob.NobId1)).IsNullable);
                Assert.False(entityType.FindProperty(nameof(Hob.NobId2)).IsNullable);
                Assert.True(entityType.GetForeignKeys().Single().IsRequired);
            }

            [Fact]
            public virtual void Nullable_FK_can_be_made_required()
            {
                var modelBuilder = HobNobBuilder();
                var principalType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .IsRequired()
                    .HasForeignKey<Nob>(e => new { e.HobId1, e.HobId2 });

                Assert.False(dependentType.FindProperty(nameof(Nob.HobId1)).IsNullable
                             && dependentType.FindProperty(nameof(Nob.HobId2)).IsNullable);
                Assert.True(dependentType.GetForeignKeys().Single().IsRequired);

                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
            }

            [Fact]
            public virtual void Non_nullable_FK_cannot_be_made_optional()
            {
                var modelBuilder = HobNobBuilder();

                Assert.Equal(
                    CoreStrings.ForeignKeyCannotBeOptional("{'NobId1', 'NobId2'}", "Hob"),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder
                        .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                        .HasForeignKey<Hob>(e => new { e.NobId1, e.NobId2 })
                        .IsRequired(false)).Message);
            }

            [Fact]
            public virtual void Non_nullable_FK_can_be_made_optional_separetely()
            {
                var modelBuilder = HobNobBuilder();

                modelBuilder
                    .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                    .HasForeignKey<Hob>(e => new { e.NobId1, e.NobId2 });

                modelBuilder
                    .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                    .IsRequired(false);

                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var fkProperty1 = dependentType.FindProperty(nameof(Hob.NobId1));
                var fkProperty2 = dependentType.FindProperty(nameof(Hob.NobId2));
                var fk = dependentType.GetForeignKeys().Single();

                Assert.False(fk.IsRequired);
                Assert.False(fkProperty1.IsNullable);
                Assert.False(fkProperty2.IsNullable);
                Assert.DoesNotContain(fkProperty1, fk.Properties);
                Assert.DoesNotContain(fkProperty2, fk.Properties);
            }

            [Fact]
            public virtual void Optional_FK_cannot_be_made_non_nullable()
            {
                var modelBuilder = HobNobBuilder();

                Assert.Equal(
                    CoreStrings.ForeignKeyCannotBeOptional("{'NobId1', 'NobId2'}", "Hob"),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder
                        .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                        .IsRequired(false)
                        .HasForeignKey<Hob>(e => new { e.NobId1, e.NobId2 })).Message);
            }

            [Fact]
            public virtual void Optional_FK_can_be_made_non_nullable_separetely()
            {
                var modelBuilder = HobNobBuilder();

                modelBuilder
                    .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                    .IsRequired(false);

                modelBuilder
                    .Entity<Nob>().HasOne(e => e.Hob).WithOne(e => e.Nob)
                    .HasForeignKey<Hob>(e => new { e.NobId1, e.NobId2 });

                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var fkProperty1 = dependentType.FindProperty(nameof(Hob.NobId1));
                var fkProperty2 = dependentType.FindProperty(nameof(Hob.NobId2));
                var fk = dependentType.GetForeignKeys().Single();

                Assert.True(fk.IsRequired);
                Assert.False(fkProperty1.IsNullable);
                Assert.False(fkProperty2.IsNullable);
                AssertEqual(new[] { fkProperty1, fkProperty2 }, fk.Properties);
            }

            [Fact]
            public virtual void Unspecified_FK_can_be_made_optional()
            {
                var modelBuilder = HobNobBuilder();
                var principalType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));
                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .IsRequired(false)
                    .HasPrincipalKey<Nob>(e => new { e.Id1, e.Id2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.False(fk.IsRequired);

                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.AddRange(fk.Properties);
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
            }

            [Fact]
            public virtual void Unspecified_FK_can_be_made_optional_in_any_order()
            {
                var modelBuilder = HobNobBuilder();
                var principalType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));
                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .HasPrincipalKey<Nob>(e => new { e.Id1, e.Id2 })
                    .IsRequired(false);

                var fk = dependentType.GetForeignKeys().Single();
                Assert.False(fk.IsRequired);

                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.AddRange(fk.Properties);
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
            }

            [Fact]
            public virtual void Unspecified_FK_can_be_made_required()
            {
                var modelBuilder = HobNobBuilder();
                var principalType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));
                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var expectedPrincipalProperties = principalType.GetProperties().ToList();
                var expectedDependentProperties = dependentType.GetProperties().ToList();

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .IsRequired()
                    .HasPrincipalKey<Nob>(e => new { e.Id1, e.Id2 });

                var fk = dependentType.GetForeignKeys().Single();
                Assert.True(fk.IsRequired);

                AssertEqual(expectedPrincipalProperties, principalType.GetProperties());
                expectedDependentProperties.AddRange(fk.Properties);
                AssertEqual(expectedDependentProperties, dependentType.GetProperties());
            }

            [Fact]
            public virtual void Can_be_defined_before_the_PK_from_principal()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Nob>();
                modelBuilder
                    .Entity<Hob>(eb =>
                        {
                            eb.HasOne(e => e.Nob).WithOne(e => e.Hob)
                                .HasForeignKey<Nob>(e => new { e.HobId1, e.HobId2 })
                                .HasPrincipalKey<Hob>(e => new { e.Id1, e.Id2 });
                            eb.HasKey(e => new { e.Id1, e.Id2 });
                        });

                modelBuilder.Entity<Nob>().HasKey(e => new { e.Id1, e.Id2 });

                var dependentEntityType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));
                var fk = dependentEntityType.GetForeignKeys().Single();
                AssertEqual(new[] { dependentEntityType.FindProperty("HobId1"), dependentEntityType.FindProperty("HobId2") }, fk.Properties);
                Assert.False(fk.IsRequired);
                var principalEntityType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                AssertEqual(fk.PrincipalKey.Properties, principalEntityType.GetKeys().Single().Properties);
            }

            [Fact]
            public virtual void Can_be_defined_before_the_PK_from_dependent()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Nob>();
                modelBuilder
                    .Entity<Hob>(eb =>
                        {
                            eb.HasOne(e => e.Nob).WithOne(e => e.Hob)
                                .HasForeignKey<Hob>(e => new { e.NobId1, e.NobId2 })
                                .HasPrincipalKey<Nob>(e => new { e.Id1, e.Id2 });
                            eb.HasKey(e => new { e.Id1, e.Id2 });
                        });

                modelBuilder.Entity<Nob>().HasKey(e => new { e.Id1, e.Id2 });

                var dependentEntityType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));
                var fk = dependentEntityType.GetForeignKeys().Single();
                AssertEqual(new[] { dependentEntityType.FindProperty("NobId1"), dependentEntityType.FindProperty("NobId2") }, fk.Properties);
                Assert.True(fk.IsRequired);
                var principalEntityType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Nob));
                AssertEqual(fk.PrincipalKey.Properties, principalEntityType.GetKeys().Single().Properties);
            }

            [Fact]
            public virtual void Can_change_delete_behavior()
            {
                var modelBuilder = HobNobBuilder();
                var dependentType = (IEntityType)modelBuilder.Model.FindEntityType(typeof(Hob));

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .OnDelete(DeleteBehavior.Cascade);

                Assert.Equal(DeleteBehavior.Cascade, dependentType.GetForeignKeys().Single().DeleteBehavior);

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .OnDelete(DeleteBehavior.Restrict);

                Assert.Equal(DeleteBehavior.Restrict, dependentType.GetForeignKeys().Single().DeleteBehavior);

                modelBuilder
                    .Entity<Hob>().HasOne(e => e.Nob).WithOne(e => e.Hob)
                    .OnDelete(DeleteBehavior.SetNull);

                Assert.Equal(DeleteBehavior.SetNull, dependentType.GetForeignKeys().Single().DeleteBehavior);
            }
        }
    }
}
