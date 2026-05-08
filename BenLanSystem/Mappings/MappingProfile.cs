using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using RouteModel = BenLanSystem.Models.Entities.Route;

namespace BenLanSystem.Mappings;

public class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        CreateMap<Location, LocationDto>();
        CreateMap<RouteModel, RouteDto>()
            .ForMember(d => d.OriginName, opt => opt.MapFrom(s => s.StartLocation.Name))
            .ForMember(d => d.DestinationName, opt => opt.MapFrom(s => s.EndLocation.Name));
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<Trip, TripDto>()
            .ForMember(d => d.OriginName, opt => opt.MapFrom(s => s.Route.StartLocation.Name))
            .ForMember(d => d.DestinationName, opt => opt.MapFrom(s => s.Route.EndLocation.Name))
            .ForMember(d => d.VehiclePlateNumber, opt => opt.MapFrom(s => s.Vehicle.PlateNumber))
            .ForMember(d => d.VehicleBrand, opt => opt.MapFrom(s => s.Vehicle.Brand))
            .ForMember(d => d.VehicleModel, opt => opt.MapFrom(s => s.Vehicle.Model));
        CreateMap<Booking, BookingDto>()
            .ForMember(d => d.OriginName, opt => opt.MapFrom(s => s.Trip.Route.StartLocation.Name))
            .ForMember(d => d.DestinationName, opt => opt.MapFrom(s => s.Trip.Route.EndLocation.Name))
            .ForMember(d => d.DepartureTimeUtc, opt => opt.MapFrom(s => s.Trip.DepartureTimeUtc));
        CreateMap<BookingPassenger, BookingPassengerDto>();
        CreateMap<Payment, PaymentDto>();
        CreateMap<Staff, StaffDto>()
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber));
        CreateMap<BlogPost, BlogPostDto>()
            .ForMember(d => d.AuthorName, opt => opt.MapFrom(s => s.Author.FirstName + " " + s.Author.LastName));
    }
}