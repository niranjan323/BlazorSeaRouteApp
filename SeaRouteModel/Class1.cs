using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeaRouteModel.Models
{
    // Short Voyage Records
    [Table("short_voyage_records")]
    public class ShortVoyageRecord : BaseClass
    {
        [Key]
        [Column("record_id")]
        public Guid RecordId { get; set; }

        [Required]
        [Column("departure_time")]
        public DateTime DepartureTime { get; set; }

        [Required]
        [Column("arrival_time")]
        public DateTime ArrivalTime { get; set; }

        [Column("forecast_time")]
        public DateTime? ForecastTime { get; set; }

        [Column("forecast_hswell")]
        public float? ForecastHswell { get; set; }

        [Column("forecast_hwind")]
        public float? ForecastHwind { get; set; }

        // Navigation property
        public virtual Record Record { get; set; }
    }

    // Season Types
    [Table("season_types")]
    public class SeasonType : BaseClass
    {
        [Key]
        [Column("season_type")]
        public byte SeasonTypeId { get; set; }

        [Required]
        [Column("season_name")]
        [StringLength(50)]
        public string SeasonName { get; set; }

        // Navigation properties
        public virtual ICollection<VoyageLegReductionFactor> VoyageLegReductionFactors { get; set; } = new List<VoyageLegReductionFactor>();
        public virtual ICollection<RecordReductionFactor> RecordReductionFactors { get; set; } = new List<RecordReductionFactor>();
    }

    // Voyage Leg Reduction Factors
    [Table("voyage_leg_reduction_factors")]
    public class VoyageLegReductionFactor : BaseClass
    {
        [Key, Column("voyage_led_id", Order = 0)]
        public Guid VoyageLegId { get; set; }

        [Key, Column("season_type", Order = 1)]
        public byte SeasonType { get; set; }

        [Column("reduction_factor")]
        public float? ReductionFactor { get; set; }

        // Navigation properties
        public virtual VoyageLeg VoyageLeg { get; set; }
        public virtual SeasonType SeasonTypeNavigation { get; set; }
    }

    // Record Reduction Factors
    [Table("record_reduction_factors")]
    public class RecordReductionFactor : BaseClass
    {
        [Key, Column("record_id", Order = 0)]
        public Guid RecordId { get; set; }

        [Key, Column("season_type", Order = 1)]
        public byte SeasonType { get; set; }

        [Required]
        [Column("reduction_factor")]
        public float ReductionFactor { get; set; }

        // Navigation properties
        public virtual Record Record { get; set; }
        public virtual SeasonType SeasonTypeNavigation { get; set; }
    }

    // Route Points
    [Table("route_points")]
    public class RoutePoint : BaseClass
    {
        [Key]
        [Column("route_point_id")]
        public Guid RoutePointId { get; set; }

        [Required]
        [Column("route_version_id")]
        public Guid RouteVersionId { get; set; }

        [Required]
        [Column("route_point_order")]
        public int RoutePointOrder { get; set; }

        [Required]
        [Column("geo_point_id")]
        public Guid GeoPointId { get; set; }

        [Required]
        [Column("seg_distance")]
        public float SegDistance { get; set; }

        // Navigation properties
        public virtual RouteVersion RouteVersion { get; set; }
        public virtual GeoPoint GeoPoint { get; set; }
    }

    // Route Versions
    [Table("route_versions")]
    public class RouteVersion : BaseClass
    {
        [Key]
        [Column("route_version_id")]
        public Guid RouteVersionId { get; set; }

        [Required]
        [Column("record_id")]
        public Guid RecordId { get; set; }

        [Required]
        [Column("record_route_version")]
        public int RecordRouteVersion { get; set; }

        // Navigation properties
        public virtual Record Record { get; set; }
        public virtual ICollection<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();
        public virtual ICollection<VoyageLeg> VoyageLegs { get; set; } = new List<VoyageLeg>();
    }

    // Voyage Legs
    [Table("voyage_legs")]
    public class VoyageLeg : BaseClass
    {
        [Key]
        [Column("voyage_leg_id")]
        public Guid VoyageLegId { get; set; }

        [Required]
        [Column("route_version_id")]
        public Guid RouteVersionId { get; set; }

        [Required]
        [Column("voyage_leg_order")]
        public int VoyageLegOrder { get; set; }

        [Column("departure_port")]
        public Guid? DeparturePort { get; set; }

        [Column("arrival_port")]
        public Guid? ArrivalPort { get; set; }

        [Column("distance")]
        public float? Distance { get; set; }

        // Navigation properties
        public virtual RouteVersion RouteVersion { get; set; }
        public virtual Port DeparturePortNavigation { get; set; }
        public virtual Port ArrivalPortNavigation { get; set; }
        public virtual ICollection<VoyageLegReductionFactor> VoyageLegReductionFactors { get; set; } = new List<VoyageLegReductionFactor>();
    }

    // Groups
    [Table("groups")]
    public class Group : BaseClass
    {
        [Key]
        [Column("group_id")]
        public Guid GroupId { get; set; }

        [Required]
        [Column("group_name")]
        [StringLength(50)]
        public string GroupName { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    }

    // Customers
    [Table("customers")]
    public class Customer : BaseClass
    {
        [Key]
        [Column("group_id")]
        public Guid GroupId { get; set; }

        [Required]
        [Column("customer_wcn")]
        [StringLength(10)]
        public string CustomerWcn { get; set; }

        [Required]
        [Column("customer_name")]
        [StringLength(100)]
        public string CustomerName { get; set; }

        // Navigation property
        public virtual Group Group { get; set; }
    }

    // Group Users
    [Table("group_users")]
    public class GroupUser : BaseClass
    {
        [Key]
        [Column("group_user_id")]
        public int GroupUserId { get; set; }

        [Required]
        [Column("group_id")]
        public Guid GroupId { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User User { get; set; }
    }

    // Record Users
    [Table("record_users")]
    public class RecordUser : BaseClass
    {
        [Key]
        [Column("record_user_id")]
        public int RecordUserId { get; set; }

        [Required]
        [Column("record_id")]
        public Guid RecordId { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        // Navigation properties
        public virtual Record Record { get; set; }
        public virtual User User { get; set; }
    }

    // Record Vessels (Note: There's a data type mismatch in the SQL - fixed here)
    [Table("record_vessels")]
    public class RecordVessel : BaseClass
    {
        [Key]
        [Column("record_vessel_id")]
        public int RecordVesselId { get; set; }

        [Required]
        [Column("record_id")]
        public Guid RecordId { get; set; }

        [Required]
        [Column("vessel_id")]
        public Guid VesselId { get; set; }

        // Navigation properties
        public virtual Record Record { get; set; }
        public virtual Vessel Vessel { get; set; }
    }

    // Placeholder classes for referenced entities (you may already have these)
    [Table("records")]
    public class Record : BaseClass
    {
        [Key]
        [Column("record_id")]
        public Guid RecordId { get; set; }

        // Add other properties as needed

        // Navigation properties
        public virtual ShortVoyageRecord ShortVoyageRecord { get; set; }
        public virtual ICollection<RecordReductionFactor> RecordReductionFactors { get; set; } = new List<RecordReductionFactor>();
        public virtual ICollection<RouteVersion> RouteVersions { get; set; } = new List<RouteVersion>();
        public virtual ICollection<RecordUser> RecordUsers { get; set; } = new List<RecordUser>();
        public virtual ICollection<RecordVessel> RecordVessels { get; set; } = new List<RecordVessel>();
    }

    [Table("geo_points")]
    public class GeoPoint : BaseClass
    {
        [Key]
        [Column("geo_point_id")]
        public Guid GeoPointId { get; set; }

        // Add other properties as needed

        // Navigation properties
        public virtual ICollection<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();
    }

    [Table("ports")]
    public class Port : BaseClass
    {
        [Key]
        [Column("geo_point_id")]
        public Guid GeoPointId { get; set; }

        // Add other properties as needed

        // Navigation properties
        public virtual ICollection<VoyageLeg> DepartureVoyageLegs { get; set; } = new List<VoyageLeg>();
        public virtual ICollection<VoyageLeg> ArrivalVoyageLegs { get; set; } = new List<VoyageLeg>();
    }

    [Table("users")]
    public class User : BaseClass
    {
        [Key]
        [Column("user_id")]
        public Guid UserId { get; set; }

        // Add other properties as needed

        // Navigation properties
        public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
        public virtual ICollection<RecordUser> RecordUsers { get; set; } = new List<RecordUser>();
    }

    [Table("vessels")]
    public class Vessel : BaseClass
    {
        [Key]
        [Column("vessel_id")]
        public Guid VesselId { get; set; }

        // Add other properties as needed

        // Navigation properties
        public virtual ICollection<RecordVessel> RecordVessels { get; set; } = new List<RecordVessel>();
    }
}