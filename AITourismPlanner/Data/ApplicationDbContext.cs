using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Models;

namespace AITourismPlanner.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // ALL DBSETS FOR YOUR 22 TABLES
        // =========================================================
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
        public DbSet<ChatbotHistory> chatbot_history { get; set; }  // Only ONE time
        public DbSet<EmergencyService> emergency_services { get; set; }
        public DbSet<AdminLog> admin_logs { get; set; }

        // =========================================================
        // MODEL CONFIGURATION
        // =========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Decimal Precision Configurations
            ConfigureDecimalPrecision(modelBuilder);

            // Relationship Configurations
            ConfigureRelationships(modelBuilder);
        }

        private void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
        {
            // Destination
            modelBuilder.Entity<Destination>()
                .Property(d => d.estimated_cost)
                .HasPrecision(10, 2);

            // Hotel
            modelBuilder.Entity<Hotel>()
                .Property(h => h.price_per_night)
                .HasPrecision(10, 2);

            // HotelRoom
            modelBuilder.Entity<HotelRoom>()
                .Property(hr => hr.room_price)
                .HasPrecision(10, 2);

            // HotelBooking
            modelBuilder.Entity<HotelBooking>()
                .Property(hb => hb.total_amount)
                .HasPrecision(10, 2);

            // Transport
            modelBuilder.Entity<Transport>()
                .Property(t => t.fare)
                .HasPrecision(10, 2);

            // TransportBooking
            modelBuilder.Entity<TransportBooking>()
                .Property(tb => tb.total_price)
                .HasPrecision(10, 2);

            // Trip
            modelBuilder.Entity<Trip>()
                .Property(t => t.total_budget)
                .HasPrecision(10, 2);

            // Itinerary
            modelBuilder.Entity<Itinerary>()
                .Property(i => i.estimated_cost)
                .HasPrecision(10, 2);

            // Payment
            modelBuilder.Entity<Payment>()
                .Property(p => p.amount)
                .HasPrecision(10, 2);

            // UserPreference
            modelBuilder.Entity<UserPreference>()
                .Property(up => up.preferred_budget)
                .HasPrecision(10, 2);
        }

        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // Review -> User (One to Many)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Review -> Destination (One to Many)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Destination)
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Review -> Hotel (One to Many)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Reviews)
                .HasForeignKey(r => r.hotel_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Favorite -> User (One to Many)
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Favorite -> Destination (One to Many)
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Destination)
                .WithMany(d => d.Favorites)
                .HasForeignKey(f => f.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Trip -> User (One to Many)
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.User)
                .WithMany(u => u.Trips)
                .HasForeignKey(t => t.user_id)
                .OnDelete(DeleteBehavior.SetNull);

            // Trip -> Destination (One to Many)
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Destination)
<<<<<<< HEAD
                .WithMany(d => d.Trips)  // This requires Trips property in Destination model
=======
                .WithMany(d => d.Trips)
>>>>>>> a4ac456814dc3b91b98b4801c838ede2ffe8cbc8
                .HasForeignKey(t => t.destination_id)
                .OnDelete(DeleteBehavior.SetNull);

            // HotelBooking -> User (One to Many)
            modelBuilder.Entity<HotelBooking>()
                .HasOne(hb => hb.User)
                .WithMany(u => u.HotelBookings)
                .HasForeignKey(hb => hb.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // HotelBooking -> Hotel (One to Many)
            modelBuilder.Entity<HotelBooking>()
                .HasOne(hb => hb.Hotel)
                .WithMany(h => h.HotelBookings)
                .HasForeignKey(hb => hb.hotel_id)
                .OnDelete(DeleteBehavior.Cascade);

            // HotelBooking -> HotelRoom (One to Many)
            modelBuilder.Entity<HotelBooking>()
                .HasOne(hb => hb.HotelRoom)
                .WithMany(hr => hr.HotelBookings)
                .HasForeignKey(hb => hb.room_id)
                .OnDelete(DeleteBehavior.SetNull);

            // TransportBooking -> User (One to Many)
            modelBuilder.Entity<TransportBooking>()
                .HasOne(tb => tb.User)
                .WithMany(u => u.TransportBookings)
                .HasForeignKey(tb => tb.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // TransportBooking -> Transport (One to Many)
            modelBuilder.Entity<TransportBooking>()
                .HasOne(tb => tb.Transport)
                .WithMany(t => t.TransportBookings)
                .HasForeignKey(tb => tb.transport_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Itinerary -> Trip (One to Many)
            modelBuilder.Entity<Itinerary>()
                .HasOne(i => i.Trip)
                .WithMany(t => t.Itineraries)
                .HasForeignKey(i => i.trip_id)
                .OnDelete(DeleteBehavior.Cascade);

            // AIRecommendation -> User (One to Many)
            modelBuilder.Entity<AIRecommendation>()
                .HasOne(ar => ar.User)
                .WithMany(u => u.AIRecommendations)
                .HasForeignKey(ar => ar.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification -> User (One to Many)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment -> User (One to Many)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatbotHistory -> User (One to Many)
            modelBuilder.Entity<ChatbotHistory>()
                .HasOne(ch => ch.User)
                .WithMany(u => u.ChatbotHistories)
                .HasForeignKey(ch => ch.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // AdminLog -> User (One to Many)
            modelBuilder.Entity<AdminLog>()
                .HasOne(al => al.Admin)
                .WithMany(u => u.AdminLogs)
                .HasForeignKey(al => al.admin_id)
                .OnDelete(DeleteBehavior.SetNull);

            // Weather -> Destination (One to Many)
            modelBuilder.Entity<Weather>()
                .HasOne(w => w.Destination)
                .WithMany(d => d.Weathers)
                .HasForeignKey(w => w.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            // EmergencyService -> Destination (One to Many)
            modelBuilder.Entity<EmergencyService>()
                .HasOne(es => es.Destination)
                .WithMany(d => d.EmergencyServices)
                .HasForeignKey(es => es.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            // DestinationImage -> Destination (One to Many)
            modelBuilder.Entity<DestinationImage>()
                .HasOne(di => di.Destination)
                .WithMany(d => d.DestinationImages)
                .HasForeignKey(di => di.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Hotel -> Destination (One to Many)
            modelBuilder.Entity<Hotel>()
                .HasOne(h => h.Destination)
                .WithMany(d => d.Hotels)
                .HasForeignKey(h => h.destination_id)
                .OnDelete(DeleteBehavior.Cascade);

            // HotelRoom -> Hotel (One to Many)
            modelBuilder.Entity<HotelRoom>()
                .HasOne(hr => hr.Hotel)
                .WithMany(h => h.HotelRooms)
                .HasForeignKey(hr => hr.hotel_id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}