using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TankDispatchManager.Models;
using Microsoft.AspNet.SignalR;
using System.Reflection;
using Newtonsoft.Json;

namespace TankDispatchManager.Controllers
{
    public class LocationController : Controller
    {
        private TankDispatcherDbEntities db = new TankDispatcherDbEntities();
        private IHubContext context = GlobalHost.ConnectionManager.GetHubContext<CommunicationHub>();

        // GET: /Contact/
        public ActionResult Index()
        {
            ViewBag.ClientName = Guid.NewGuid();
            return View();
        }

        public ActionResult Selection(string id = "")
        {
            ViewBag.ClientName = Guid.NewGuid();
            ViewBag.CurrentSelection = id;
            return View();
        }

        public PartialViewResult Table()
        {
            return new PartialViewResult();
        }

        public async Task<JsonResult> AssignIds(string id)
        {
            string sele = Request["data"];
            Guid gId = new Guid(id);
            foreach(ContactLocationMapping map in db.ContactLocationMappings.Where(x => x.Location == gId))
            {
                db.ContactLocationMappings.Remove(map);
            }
            await db.SaveChangesAsync();
            foreach(string s in sele.Split(','))
            {
                if(s == "")
                {
                    continue;
                }
                ContactLocationMapping mapping = new ContactLocationMapping();
                mapping.Id = Guid.NewGuid();
                mapping.Location = gId;
                mapping.Contact = new Guid(s);
                db.ContactLocationMappings.Add(mapping);
            }
            await db.SaveChangesAsync();
            await Task.Run(() =>
            {
                var clients = context.Clients.All;
                clients.refreshChildTable(id);
            });
            return Json(new { Result = "OK" });
        }

        // POST: /Contact/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<JsonResult> CreateLocation([Bind(Include = "Id,WellName,Tanks,Pumper,Transporter,District,State,County,P8RequestDate,ApprovedBarrels,BarrelsSold,BOPD,P8IncreaseRequested")] Location location)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { Result = "ERROR", Message = "Form is not Valid! Please correct it and try again." });
                }
                location.Id = Guid.NewGuid();
                Location loc = db.Locations.Add(location);
                await db.SaveChangesAsync();
                LiteLocation addedLocation = new LiteLocation(loc);
                var clientName = Request["ClientName"];
                await Task.Run(() =>
                {
                    var clients = context.Clients.All;
                    clients.createLocation(clientName, addedLocation);
                });
                return Json(new { Result = "OK", Record = addedLocation });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateLocation([Bind(Include = "Id,WellName,Tanks,Pumper,Transporter,District,State,County,P8RequestDate,ApprovedBarrels,BarrelsSold,BOPD,P8IncreaseRequested")] Location location)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { Result = "ERROR", Message = "Form is not Valid! Please correct it and try again." });
                }
                db.Entry(location).State = EntityState.Modified;
                await db.SaveChangesAsync();
                LiteLocation loc = new LiteLocation(location);
                var clientName = Request["ClientName"];
                await Task.Run(() =>
                {
                    var clients = context.Clients.All;
                    clients.updateLocation(clientName, loc);
                });
                return Json(new { Result = "OK", Record = loc });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteLocation(string id)
        {
            try
            {
                if (id == null)
                {
                    return Json(new { Result = "ERROR", Message = "An item with that id does not exist." });
                }
                db.Locations.Remove(await db.Locations.FindAsync(new Guid(id)));
                await db.SaveChangesAsync();
                var clientName = Request["ClientName"];
                await Task.Run(() =>
                {
                    var clients = context.Clients.All;
                    clients.deleteLocation(clientName, id);
                });
                return Json(new { Result = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult List(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = "", string data = "")
        {
            try
            {
                FilterLocationData filter = new FilterLocationData();
                if(data != "")
                {
                    filter = JsonConvert.DeserializeObject<FilterLocationData>(data);
                }
                IQueryable<Location> locations = FilterResults(filter);
                IEnumerable<Location> locationList = SortedList(locations, jtStartIndex, jtPageSize, jtSorting);
                List<LiteLocation> loc = new List<LiteLocation>();
                foreach (Location l in locationList)
                {
                    loc.Add(new LiteLocation(l));
                }
                return Json(new { Result = "OK", Records =  loc, TotalRecordCount = locations.Count() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        private IEnumerable<Location> SortedList(IQueryable<Location> locations, int jtStartIndex, int jtPageSize, string sort = "")
        {
            if (sort != "")
            {
                string[] sortParts = sort.Split(' ');
                PropertyInfo info = null;
                string ordering = sortParts[1] == "ASC" ? " ascending" : " descending";
                if ((info = typeof(Location).GetProperty(sortParts[0])) != null)
                {
                    return locations.OrderBy("it." + info.Name + ordering).Skip(jtStartIndex).Take(jtPageSize).ToList();
                }
                else if ((info = typeof(Contact).GetProperty(sortParts[0])) != null)
                {
                    return locations.OrderBy("it.PumperContact." + info.Name + ordering).Skip(jtStartIndex).Take(jtPageSize).ToList();
                }
            }
            return locations.OrderBy(x => x.WellName).Skip(jtStartIndex).Take(jtPageSize).ToList();
        }

        private IQueryable<Location> FilterResults(FilterLocationData filter)
        {
            DateTime od;
            int oi;
            IQueryable<Location> locations = db.Locations.Include(x => x.PumperContact);
            if (filter.WellName != "")
            {
                locations = locations.Where(x => x.WellName.Contains(filter.WellName));
            }
            if (filter.Tanks != "")
            {
                locations = locations.Where(x => x.Tanks.Contains(filter.Tanks + ",") || x.Tanks.Contains("," + filter.Tanks));
            }
            if (filter.Pumper != "")
            {
                locations = locations.Where(x => x.PumperContact.Name.Contains(filter.Pumper));
            }
            if (filter.Transporter != "")
            {
                locations = locations.Where(x => x.Transporter.Contains(filter.Transporter));
            }
            if (filter.District != "")
            {
                locations = locations.Where(x => x.District.Contains(filter.District));
            }
            if (filter.State != "")
            {
                locations = locations.Where(x => x.State.Contains(filter.State));
            }
            if (filter.County != "")
            {
                locations = locations.Where(x => x.County.Contains(filter.County));
            }
            if (filter.P8RequestDate != "")
            {
                if (DateTime.TryParse(filter.P8RequestDate, out od))
                {
                    locations = locations.Where(x => x.P8RequestDate.Value == od);
                }
            }
            if (filter.ApprovedBarrels != "")
            {
                if(filter.ApprovedBarrels.Equals("infinity", StringComparison.InvariantCultureIgnoreCase))
                {
                    locations = locations.Where(x => x.ApprovedBarrels == -1);
                }
                else if(int.TryParse(filter.ApprovedBarrels, out oi))
                {
                    locations = locations.Where(x => x.ApprovedBarrels == oi);
                }
            }
            if (filter.BarrelsSold != "")
            {
                if (int.TryParse(filter.ApprovedBarrels, out oi))
                {
                    locations = locations.Where(x => x.BarrelsSold == oi);
                }
            }
            if (filter.BOPD != "")
            {
                if (int.TryParse(filter.BOPD, out oi))
                {
                    locations = locations.Where(x => x.BOPD == oi);
                }
            }
            if (filter.P8IncreaseRequested != "")
            {
                if (filter.P8IncreaseRequested == "true")
                {
                    locations = locations.Where(x => x.P8IncreaseRequested.Value == true);
                }
                else if(filter.P8IncreaseRequested == "false")
                {
                    locations = locations.Where(x => x.P8IncreaseRequested.Value == false);
                }
            }
            return locations;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private class LiteLocation
        {
            private TankDispatcherDbEntities db = new TankDispatcherDbEntities();

            public Guid Id { get; set; }
            public string WellName { get; set; }
            public string Tanks { get; set; }
            public Guid? PumperId { get; set; }
            public string Pumper { get; set; }
            public string Transporter { get; set; }
            public string District { get; set; }
            public string State { get; set; }
            public string County { get; set; }
            public string P8RequestDate { get; set; }
            public Nullable<int> ApprovedBarrels { get; set; }
            public Nullable<int> BarrelsSold { get; set; }
            public Nullable<int> BOPD { get; set; }
            public Nullable<bool> P8IncreaseRequested { get; set; }

            public LiteLocation (Location loc)
            {
                Id = loc.Id;
                WellName = loc.WellName;
                Tanks = loc.Tanks;
                PumperId = loc.Pumper;
                Transporter = loc.Transporter;
                District = loc.District;
                State = loc.State;
                County = loc.County;
                Pumper = null;
                if (PumperId != null)
                {
                    if (loc.PumperContact != null)
                    {
                        Pumper = loc.PumperContact.Name;
                    }
                    else
                    {
                        Pumper = db.Contacts.Find(PumperId).Name;
                    }
                }
                P8RequestDate = loc.P8RequestDate.HasValue ? loc.P8RequestDate.Value.ToString("yyyy-MM-dd") : null;
                ApprovedBarrels = loc.ApprovedBarrels;
                BarrelsSold = loc.BarrelsSold;
                BOPD = loc.BOPD;
                P8IncreaseRequested = loc.P8IncreaseRequested.HasValue ? loc.P8IncreaseRequested.Value : false;
            }
        }

        private class FilterLocationData
        {
            public string WellName { get; set; }
            public string Tanks { get; set; }
            public string Pumper { get; set; }
            public string Transporter { get; set; }
            public string District { get; set; }
            public string State { get; set; }
            public string County { get; set; }
            public string P8RequestDate { get; set; }
            public string ApprovedBarrels { get; set; }
            public string BarrelsSold { get; set; }
            public string BOPD { get; set; }
            public string P8IncreaseRequested { get; set; }

            public FilterLocationData()
            {
                WellName = "";
                Tanks = "";
                Pumper = "";
                Transporter = "";
                District = "";
                State = "";
                County = "";
                P8RequestDate = "";
                ApprovedBarrels = "";
                BarrelsSold = "";
                BOPD = "";
                P8IncreaseRequested = "";
            }
        }
    }
}