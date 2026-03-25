using AgroClassLib.FarmOwner;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace GSTAgroTourism.Controllers
{
    public class FarmOwnerController : Controller
    {

        // For loading the login page (GET request)
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }


        // For loading the default landing page
        public ActionResult Index()
        {
            return View();
        }


        // For authenticating user login (FarmOwner or Visitor)
        [HttpPost]
        public async Task<ActionResult> Login(LoginRS model, string returnUrl)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();
            DataSet ds = await objbalfarm.Login(model);

            if (ds.Tables[0].Rows.Count > 0)
            {
                // OWNER LOGIN
                Session["UserId"] = ds.Tables[0].Rows[0]["UserId"];
                Session["Email"] = ds.Tables[0].Rows[0]["Email"];
                Session["FarmOwnerCode"] = ds.Tables[0].Rows[0]["FarmOwnerCode"].ToString();

                return RedirectToAction("ActivityAG", "FarmOwner");
            }
            else if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
            {
                // VISITOR
                Session["UserId"] = ds.Tables[1].Rows[0]["UserId"];
                Session["Email"] = ds.Tables[1].Rows[0]["Email"];
                Session["VisitorCode"] = ds.Tables[1].Rows[0]["VisitorCode"];
                Session["VisitorName"] = ds.Tables[1].Rows[0]["FullName"];
                TempData["Login"] = "Success";

                //        // 🔥 IMPORTANT PART
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("AboutUs", "Visitor");
            }
            else
            {
                ViewBag.Message = "Invalid Email or Password";
                return View("Index");
            }
        }

        #region Atharva
        // For displaying all activities and farmhouse dropdown for the logged-in owner
        public async Task<ActionResult> ActivityAG()
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            string ownerCode = Session["FarmOwnerCode"].ToString();

            DataSet farmDs = await objbalfarm.GetFarmHouseByOwnerAG(ownerCode);

            List<SelectListItem> farmList = new List<SelectListItem>();

            foreach (DataRow dr in farmDs.Tables[0].Rows)
            {
                farmList.Add(new SelectListItem
                {
                    Value = dr["FarmHouseCode"].ToString(),
                    Text = dr["FarmHouseName"].ToString()
                });
            }

            ViewBag.FarmHouseList = farmList;

            DataSet ds = await objbalfarm.GetActivitiesAG(ownerCode, null);

            List<FarmActivitiesAG> list = new List<FarmActivitiesAG>();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    list.Add(new FarmActivitiesAG
                    {
                        ActivityId = Convert.ToInt32(dr["ActivityId"]),
                        ActivityCode = dr["ActivityCode"].ToString(),
                        FarmHouseCode = dr["FarmHouseCode"].ToString(),
                        FarmHouseName = dr["FarmHouseName"].ToString(),
                        ActivityName = dr["ActivityName"].ToString(),
                        Duration = dr["Duration"].ToString(),
                        Price = Convert.ToDecimal(dr["Price"]),
                        StartDate = dr["StartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["StartDate"]),
                        EndDate = dr["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["EndDate"]),
                        Description = dr["Description"].ToString(),
                        ImagePath = dr["ImagePath"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    });
                }
            }

            return View(list);
        }


        // For inserting a new activity with image upload
        [HttpPost]
        public async Task<JsonResult> InsertActivityAG(FarmActivitiesAG obj, HttpPostedFileBase ImageFile)
        {
            if (!obj.StartDate.HasValue || !obj.EndDate.HasValue)
            {
                return Json(new { success = false, message = "Dates are required" });
            }

            if (obj.StartDate.Value.Date < DateTime.Today)
            {
                return Json(new { success = false, message = "Start date cannot be before today" });
            }

            if (obj.EndDate.Value.Date < obj.StartDate.Value.Date)
            {
                return Json(new { success = false, message = "End date must be after start date" });
            }
            BALFarmOwner objbalfarm = new BALFarmOwner();

            obj.ActivityCode = "AC" + DateTime.Now.Ticks.ToString().Substring(8);

            obj.ImagePath = "";

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string ext = Path.GetExtension(ImageFile.FileName).ToLower();

                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    return Json(new { success = false, message = "Only JPG, JPEG, PNG allowed" });
                }

                string rootPath = Server.MapPath("~/Content/Image/Farms/");
                string farmFolder = Path.Combine(rootPath, obj.FarmHouseCode);

                if (!Directory.Exists(farmFolder))
                {
                    Directory.CreateDirectory(farmFolder);
                }

                string fileName = obj.ActivityCode + "_" + Path.GetFileName(ImageFile.FileName);
                string fullPath = Path.Combine(farmFolder, fileName);

                ImageFile.SaveAs(fullPath);

                obj.ImagePath = "/Content/Image/Farms/" + obj.FarmHouseCode + "/" + fileName;
            }

            await objbalfarm.InsertActivityAG(obj);

            return Json(new { success = true });
        }


        // For fetching a specific activity by ID (used in Edit modal)
        public async Task<JsonResult> GetActivityByIdAG(int id)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            var activity = await objbalfarm.GetActivityByIdAG(id);

            return Json(new
            {
                activity.ActivityId,
                activity.ActivityCode,
                activity.FarmHouseCode,
                activity.ActivityName,
                activity.Duration,
                activity.Price,
                StartDate = activity.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = activity.EndDate?.ToString("yyyy-MM-dd"),
                activity.Description,
                activity.ImagePath
            }, JsonRequestBehavior.AllowGet);
        }


        // For updating an existing activity and optionally replacing the image
        [HttpPost]
        public async Task<JsonResult> UpdateActivityAG(FarmActivitiesAG obj, HttpPostedFileBase ImageFile)
        {
            if (!obj.StartDate.HasValue || !obj.EndDate.HasValue)
            {
                return Json(new { success = false, message = "Dates are required" });
            }

            if (obj.StartDate.Value.Date < DateTime.Today)
            {
                return Json(new { success = false, message = "Start date cannot be before today" });
            }

            if (obj.EndDate.Value.Date < obj.StartDate.Value.Date)
            {
                return Json(new { success = false, message = "End date must be after start date" });
            }

            BALFarmOwner objbalfarm = new BALFarmOwner();

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string ext = Path.GetExtension(ImageFile.FileName).ToLower();

                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    return Json(false);
                }

                string rootPath = Server.MapPath("~/Content/Image/Farms/");
                string farmFolder = Path.Combine(rootPath, obj.FarmHouseCode);

                if (!Directory.Exists(farmFolder))
                {
                    Directory.CreateDirectory(farmFolder);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);

                string fullPath = Path.Combine(farmFolder, fileName);

                ImageFile.SaveAs(fullPath);

                obj.ImagePath = "/Content/Image/Farms/" + obj.FarmHouseCode + "/" + fileName;
            }

            await objbalfarm.UpdateActivityAG(obj);

            return Json(new { success = true });
        }


        // For filtering activities based on selected farmhouse (AJAX request)
        public async Task<JsonResult> GetActivitiesByFarmAG(string farmHouseCode)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            string ownerCode = Session["FarmOwnerCode"].ToString();

            DataSet ds = await objbalfarm.GetActivitiesAG(ownerCode, farmHouseCode);

            return Json(ds.Tables[0], JsonRequestBehavior.AllowGet);
        }


        // For deleting an activity
        [HttpPost]
        public async Task<JsonResult> DeleteActivityAG(int id)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            await objbalfarm.DeleteActivityAG(id);

            return Json(true);
        }


        // For activating or deactivating an activity (toggle status)
        [HttpPost]
        public async Task<JsonResult> ToggleActivityAG(int id, bool isActive)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();
            await objbalfarm.ToggleActivityAG(id, isActive);
            return Json(true);
        }
        #endregion
    }
}