using AITourismPlanner.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Xml;

namespace AITourismPlanner.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // All DbSets for your 22 tables
        public DbSet<Role> roles { get; set; }
        public DbSet<User> users { get; set; }
        public DbSet<UserPreference> user_preferences { get; set; }
        public DbSet<Category> categories { get; set; }
        public DbSet<Destination> destinations { get; set; }
        public DbSet<DestinationImage> destination_images { get; set; }
        public DbSet<Hotel> hotels { get; set; }
        public DbSet<HotelRoom> hotel_rooms { get; set; }
        public DbSet<HotelBooking> hotel_bookings { get; set; }
        public DbSet<Transport> transports { get; set; }
        public DbSet<TransportBooking> transport_bookings { get; set; }
        public DbSet<Trip> trips { get; set; }
        public DbSet<Itinerary> itineraries { get; set; }
        public DbSet<AIRecommendation> ai_recommendations { get; set; }
        public DbSet<Review> reviews { get; set; }
        public DbSet<Favorite> favorites { get; set; }
        public DbSet<Weather> weather { get; set; }
        public DbSet<Payment> payments { get; set; }
        public DbSet<Notification> notifications { get; set; }
        public DbSet<ChatbotHistory> chatbot_history { get; set; }
        public DbSet<EmergencyService> emergency_services { get; set; }
        public DbSet<AdminLog> admin_logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<Destination>()
                .Property(d => d.estimated_cost)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Hotel>()
                .Property(h => h.price_per_night)
                .HasPrecision(10, 2);

            modelBuilder.Entity<HotelRoom>()
                .Property(hr => hr.room_price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<HotelBooking>()
                .Property(hb => hb.total_amount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Transport>()
                .Property(t => t.fare)
                .HasPrecision(10, 2);

            modelBuilder.Entity<TransportBooking>()
                .Property(tb => tb.total_price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Trip>()
                .Property(t => t.total_budget)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Itinerary>()
                .Property(i => i.estimated_cost)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.amount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<UserPreference>()
                .Property(up => up.preferred_budget)
                .HasPrecision(10, 2);

            // Configure relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Destination)
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.user_id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}