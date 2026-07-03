# Danh Sách File Liên Quan Đến "Setup Promotion" và "Promotion" - Portal.RPOS

**Ngày tạo:** 2026-05-27  
**Project:** VCM.BLUEPOS (Portal.RPOS)  
**Mục đích:** Liệt kê tất cả các file, folder, và component liên quan đến tính năng Khuyến Mãi (Setup Promotion / Promotion / Khuyen Mai)

---

## 📌 TÓM TẮT CẤU TRÚC

Tính năng Promotion được chia làm **2 module chính**:

1. **SetupPromotion** - Thiết lập / Quản lý khuyến mãi (Admin)
2. **Promotion** - Xem/Kiểm tra khuyến mãi (POS/Frontend)

---

## 🗂️ DANH SÁCH FILE CHI TIẾT

### 1️⃣ **SETUP PROMOTION MODULE** (Khuyến Mãi - Setup)

#### **📁 Controllers** (VCM.BLUEPOS/Controllers/)
```
SetupPromotionController.cs
├─ Namespace: PLG.Controllers
├─ Base: BaseController
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Controllers\SetupPromotionController.cs
└─ Mô tả: Quản lý API setup khuyến mãi (CRUD offer header, buy, get, site)
```

#### **📁 Business Logic (VCM.BLUEPOS.Business/SetupPromotion/)**
```
SetupPromotionBLO.cs
├─ Namespace: VCM.BLUEPOS.Business.SetupPromotion
├─ Interface: ISetupPromotionBLO (implicit)
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Business\SetupPromotion\SetupPromotionBLO.cs
└─ Mô tả: Business logic xử lý logic thiết lập khuyến mãi
```

#### **📁 Data Access (VCM.BLUEPOS.Data/SetupPromotion/)**
```
SetupPromotionData.cs
├─ Namespace: VCM.BLUEPOS.Data.SetupPromotion
├─ Interface: ISetupPromotionData (implicit)
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Data\SetupPromotion\SetupPromotionData.cs
└─ Mô tả: Data access layer truy cập database khuyến mãi
```

#### **📁 Entity Framework - EF Entities (VCM.BLUEPOS.Data/EF/Central/)**
```
SetupPromotionBUY.cs
├─ Class: SetupPromotionBUY (Entity)
├─ DbContext: CentralMDPartner
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Data\EF\Central\SetupPromotionBUY.cs
└─ Mô tả: Entity mapping table SETUPPROMOTION_BUY

SetupPromotionGET.cs
├─ Class: SetupPromotionGET (Entity)
├─ DbContext: CentralMDPartner
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Data\EF\Central\SetupPromotionGET.cs
└─ Mô tả: Entity mapping table SETUPPROMOTION_GET

SetupPromotionHEADER.cs
├─ Class: SetupPromotionHEADER (Entity)
├─ DbContext: CentralMDPartner
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Data\EF\Central\SetupPromotionHEADER.cs
└─ Mô tả: Entity mapping table SETUPPROMOTION_HEADER (Offer Header)

SetupPromotionSITE.cs
├─ Class: SetupPromotionSITE (Entity)
├─ DbContext: CentralMDPartner
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Data\EF\Central\SetupPromotionSITE.cs
└─ Mô tả: Entity mapping table SETUPPROMOTION_SITE
```

#### **📁 Models (VCM.BLUEPOS.Model/SetupPromotion/)**
**Tổng: 30 model files**

**Main Setup Models:**
- `SetupPromotionHEADERModel.cs` - DTO cho Offer Header
- `SetupPromotionBUYModel.cs` - DTO cho Offer Buy (Mua lượng lớn)
- `SetupPromotionGETModel.cs` - DTO cho Offer Get (Mua được)
- `SetupPromotionSITEModel.cs` - DTO cho Offer Site (Cửa hàng/Chi nhánh)
- `SetupMainModel.cs` - Main setup model (combine)

**Offer Models:**
- `OfferHeaderModel.cs` - Offer Header model
- `OfferBuyModel.cs` - Offer Buy model
- `OfferGetModel.cs` - Offer Get model
- `OfferSiteModel.cs` - Offer Site model
- `OfferTypeModel.cs` - Offer Type enum/model

**Supporting Models:**
- `AdvanceSetupModel.cs` - Advanced setup options
- `SetupBuyModel.cs` - Setup Buy model
- `SetupSpecialComboModel.cs` - Special combo setup
- `StoreSetupModel.cs` - Store setup model
- `ViewSetupCHSTModel.cs` - View setup model
- `ComboboxModel.cs` - Combobox options
- `GroupSiteModel.cs` - Group of sites
- `SalesTypeModel.cs` - Sales type
- `OfferTypeModel.cs` - Offer type
- `DayInWeekModel.cs` - Day in week
- `ItemSetupMode.cs` - Item setup mode
- `BuyGroupItemModel.cs` - Buy group item
- `GetBBModel.cs` - Get BB (Buy Best) model
- `CodeBySpecialComboHeaderModel.cs` - Code by special combo
- `ItemByComboLineModel.cs` - Item by combo line
- `AppendOfferBuyModel.cs` - Append offer buy
- `AppendOfferGetModel.cs` - Append offer get
- `ViewDetailOfferSiteModel.cs` - View detail offer site
- `ViewItemRequestModel.cs` - View item request
- `ViewSetupGroupItemBuyModel.cs` - View setup group item buy
- `ViewTabOfferGetModel.cs` - View tab offer get
- `_ViewDataBuyGroupItem.cshtml` - View data buy group item
- `_ViewDataGetGroupItem.cshtml` - View data get group item
- `_ViewListItemSetup.cshtml` - View list item setup

**File paths:**
```
d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\SetupPromotion\{model_name}.cs
```

#### **📁 Views (VCM.BLUEPOS/Views/SetupPromotion/)**
**Tổng: 38+ view files (cshtml)**

**Main Views:**
- `SetupMain.cshtml` - Main setup page
- `SetupSpecialComboList.cshtml` - Special combo list page

**Partial Views (Modal/Dialogs):**
- `_AppendOfferBuy.cshtml` - Append offer buy modal
- `_AppendOfferGet.cshtml` - Append offer get modal
- `_DetailOfferBuy.cshtml` - Detail offer buy modal
- `_DetailOfferGet.cshtml` - Detail offer get modal
- `_DetailOfferHeader.cshtml` - Detail offer header modal
- `_DetailOfferSite.cshtml` - Detail offer site modal
- `_SetupAdvance.cshtml` - Advanced setup modal

**Data/List Views:**
- `_ViewListOfferHeader.cshtml` - List offer header
- `_ViewListIem.cshtml` - List item (Buy)
- `_ViewListIemGet.cshtml` - List item (Get)
- `_ViewListStore.cshtml` - List store
- `_ViewListStoreSetup.cshtml` - List store setup
- `_ViewListItemSetup.cshtml` - List item setup
- `_ViewListItemGetSetup.cshtml` - List item get setup

**Support Views:**
- `_ViewDataGroupSite.cshtml` - Group site data
- `_ViewDataBuyGroupItem.cshtml` - Buy group item data
- `_ViewDataGetGroupItem.cshtml` - Get group item data
- `_ViewSelectGroupSite.cshtml` - Select group site
- `_ViewSetupCHST.cshtml` - Setup CHST (Combo Hàng Siêu Tiêu)
- `_ViewSetupGroupItemBuy.cshtml` - Setup group item buy
- `_ViewSetupGroupItemGet.cshtml` - Setup group item get
- `_AddStoreGroup_ViewDataGroupSite.cshtml` - Add store group

**Backup Files (không dùng):**
- `SetupSpecialComboList_backupV1-V4.cshtml` - Backup versions
- `_AppendOfferBuy - Copy.cshtml` - Copy file
- `_DetailOfferBuy - Copy.cshtml` - Copy files
- v.v...

**File paths:**
```
d:\Projects\Portal.RPOS\VCM.BLUEPOS\Views\SetupPromotion\{view_name}.cshtml
```

#### **📁 JavaScript (VCM.BLUEPOS/Content/)**
```
setuppromotion.js
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Content\setuppromotion.js
├─ Routes được gọi:
│  ├─ SetupPromotion/GetItem
│  ├─ SetupPromotion/_DetailOfferBuy
│  ├─ SetupPromotion/_DetailOfferGet
│  ├─ SetupPromotion/_DetailOfferHeader
│  ├─ SetupPromotion/InsertUpdateOfferBuy
│  ├─ SetupPromotion/InsertUpdateOfferGet
│  └─ ... (xem file để chi tiết)
└─ Mô tả: JavaScript handler cho UI Setup Promotion
```

---

### 2️⃣ **PROMOTION MODULE** (Khuyến Mãi - View/Report)

#### **📁 Controllers (VCM.BLUEPOS/Controllers/)**
```
PromotionController.cs
├─ Namespace: PLG.Controllers
├─ Base: BaseController
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Controllers\PromotionController.cs
└─ Mô tả: API xem/kiểm tra khuyến mãi (không edit)
```

#### **📁 Business Logic (VCM.BLUEPOS.Business/Promotion/)**
```
PromotionBLO.cs
├─ Namespace: VCM.BLUEPOS.Business.Promotion
├─ Interface: IPromotionBLO (implicit)
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Business\Promotion\PromotionBLO.cs
└─ Mô tả: Business logic xử lý xem/kiểm tra khuyến mãi
```

#### **📁 Data Access (VCM.BLUEPOS.Data/Promotion/)**
```
PromotionData.cs
├─ Namespace: VCM.BLUEPOS.Data.Promotion
├─ Interface: IPromotionData (implicit)
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Data\Promotion\PromotionData.cs
└─ Mô tả: Data access layer truy cập promotion data
```

#### **📁 Models (VCM.BLUEPOS.Model/Promotion/)**
**Tổng: 4 model files**

```
PromotionModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Promotion\PromotionModel.cs
└─ Mô tả: Main promotion model

ExportExcel_PromotionOfferBuyModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Promotion\ExportExcel_PromotionOfferBuyModel.cs
└─ Mô tả: Export promotion offer buy model (Excel export)

ExportExcel_PromotionOfferGetModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Promotion\ExportExcel_PromotionOfferGetModel.cs
└─ Mô tả: Export promotion offer get model (Excel export)

ExportExcel_PromotionOfferSiteModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Promotion\ExportExcel_PromotionOfferSiteModel.cs
└─ Mô tả: Export promotion offer site model (Excel export)
```

#### **📁 Views (VCM.BLUEPOS/Views/Promotion/)**
**Tổng: 3 view files**

```
PromotionList.cshtml
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Views\Promotion\PromotionList.cshtml
├─ @using: VCM.BLUEPOS.Model.Promotion, VCM.BLUEPOS.Model.SetupPromotion
└─ Mô tả: Main promotion list page (UI danh sách khuyến mãi)

CheckPromotionList.cshtml
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Views\Promotion\CheckPromotionList.cshtml
└─ Mô tả: Check promotion list page

_ViewPromotionCheckForItem.cshtml
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Views\Promotion\_ViewPromotionCheckForItem.cshtml
├─ @using: VCM.BLUEPOS.Model.Promotion
├─ Model Type: List<ViewCheckPromotionModalModel>
└─ Mô tả: Partial view modal kiểm tra khuyến mãi cho từng item
```

---

### 3️⃣ **RELATED MODELS - REPORT & OTHER MODULES**

#### **📁 Report Models (VCM.BLUEPOS.Model/Report/)**
```
OfferTypePromotionModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Report\OfferTypePromotionModel.cs
└─ Mô tả: Report model cho loại offer khuyến mãi

PromotionOfferTypeComboModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Report\PromotionOfferTypeComboModel.cs
└─ Mô tả: Combo model promotion offer type

SalesTypePromotionModel.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Report\SalesTypePromotionModel.cs
└─ Mô tả: Sales type promotion report model
```

#### **📁 Order Related Models (VCM.BLUEPOS.Model/Order/)**
```
PrintInvoiceOrderSalesModel.cs (chứa PromotionInforModel)
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Model\Order\PrintInvoiceOrderSalesModel.cs
├─ Inner Class: PromotionInforModel
└─ Mô tả: Model in hoá đơn order có thông tin promotion

ViewDetailPromotionVoucherCouponModel.cs (trong Order BLO)
├─ Namespace: VCM.BLUEPOS.Model.Order
└─ Mô tả: Model chi tiết promotion voucher coupon
```

#### **📁 Order Business Logic**
```
OrderSalesPrintBLO.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Business\Order\OrderSalesPrintBLO.cs
├─ Method: GetListPromotionByPrintInvoiceOrderSales()
└─ Mô tả: Lấy danh sách promotion cho print invoice

OrderBLO.cs
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS.Business\Order\OrderBLO.cs
├─ Methods:
│  ├─ GetOrderDetailPromotionList()
│  ├─ GetOrderDetailPromotionByPosterminal()
│  └─ Export_Get_Detail_Promotion_List_By_Posterminal()
└─ Mô tả: Xử lý promotion trong order sales
```

---

### 4️⃣ **RELATED MODULES - SETUP COUPON & VOUCHER**

#### **📁 SetupCoupon Views (VCM.BLUEPOS/Views/SetupCoupon/)**
```
_SetupCoupon.cshtml
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Views\SetupCoupon\_SetupCoupon.cshtml
├─ @using: VCM.BLUEPOS.Model.SetupPromotion
└─ Mô tả: Partial view setup coupon (liên quan setup promotion)

_SetupCouponVoucher.cshtml
├─ Đường dẫn: d:\Projects\Portal.RPOS\VCM.BLUEPOS\Views\SetupCoupon\_SetupCouponVoucher.cshtml
├─ @using: VCM.BLUEPOS.Model.SetupPromotion
└─ Mô tả: Partial view setup coupon voucher
```

---

## 📊 TỔNG HỢP THỐNG KÊ

| Loại | Số lượng | Ghi chú |
|------|---------|--------|
| **Controller** | 2 | SetupPromotionController, PromotionController |
| **BLO (Business Logic)** | 2 | SetupPromotionBLO, PromotionBLO |
| **Data Access** | 2 | SetupPromotionData, PromotionData |
| **EF Entities** | 4 | SetupPromotionBUY, GET, HEADER, SITE |
| **Models (SetupPromotion)** | 30 | Main, Offer, Supporting models |
| **Models (Promotion)** | 4 | PromotionModel, Export models (3) |
| **Models (Report)** | 3 | OfferTypePromotion, etc. |
| **Views (SetupPromotion)** | 40+ | Main + Partial + Backup |
| **Views (Promotion)** | 3 | PromotionList, CheckPromotion, Modal |
| **JavaScript** | 1 | setuppromotion.js |
| **SetupCoupon Related** | 2 | _SetupCoupon, _SetupCouponVoucher |
| **Total Files** | **~95** | Tổng tất cả file liên quan |

---

## 🔗 DEPENDENCY DIAGRAM

```
SetupPromotionController
    ├─→ SetupPromotionBLO (ISetupPromotionBLO)
    │   ├─→ SetupPromotionData (ISetupPromotionData)
    │   │   ├─→ DbContext (CentralMDPartner)
    │   │   │   ├─→ SetupPromotionBUY
    │   │   │   ├─→ SetupPromotionGET
    │   │   │   ├─→ SetupPromotionHEADER
    │   │   │   └─→ SetupPromotionSITE
    │   │   └─→ Models
    │   └─→ Models (SetupPromotion/*)
    └─→ Views (SetupPromotion/*)
        ├─→ setuppromotion.js
        └─→ Models

PromotionController
    ├─→ PromotionBLO (IPromotionBLO)
    │   ├─→ PromotionData (IPromotionData)
    │   │   └─→ DbContext
    │   └─→ Models (Promotion/*)
    └─→ Views (Promotion/*)
        └─→ Models

SetupCouponController
    └─→ Views (SetupCoupon/*)
        └─→ Models (SetupPromotion/*)
            └─→ [Liên kết tới SetupPromotion]
```

---

## 🎯 CHỨC NĂNG CHÍNH

### **SetupPromotion** (Thiết lập Khuyến Mãi)
- ✅ Tạo mới / Sửa / Xóa Offer Header (Tiêu đề khuyến mãi)
- ✅ Quản lý Offer Buy (Điều kiện mua)
- ✅ Quản lý Offer Get (Lợi ích nhận được)
- ✅ Quản lý Offer Site (Cửa hàng áp dụng)
- ✅ Special Combo setup
- ✅ Advanced setup options

### **Promotion** (Xem/Kiểm tra Khuyến Mãi)
- ✅ Xem danh sách khuyến mãi
- ✅ Kiểm tra khuyến mãi cho từng item
- ✅ Export promotion data
- ✅ Modal view chi tiết khuyến mãi

### **Related** (Liên quan)
- ✅ Promotion info trong Order/Invoice
- ✅ Promotion report & statistics
- ✅ Coupon & Voucher management

---

## 📝 GHI CHÚ

1. **Architectural Pattern**: Layered Architecture (Model → Controller → Business → Data → EF)
2. **DI Container**: Autofac (configured in AutofacConfig.cs)
3. **Framework**: ASP.NET MVC 5.2 (.NET Framework 4.8)
4. **ORM**: Entity Framework 6.x
5. **Frontend**: Bootstrap 3.3.7 + jQuery 3.3.1 + Metronic theme
6. **DB**: SQL Server (DbContext name: CentralMDPartner)

---

## 🔗 LIÊN KẾT NHANH

### Các Folder Chính
- Model: `VCM.BLUEPOS.Model\SetupPromotion\`
- Model: `VCM.BLUEPOS.Model\Promotion\`
- Business: `VCM.BLUEPOS.Business\SetupPromotion\`
- Business: `VCM.BLUEPOS.Business\Promotion\`
- Data: `VCM.BLUEPOS.Data\SetupPromotion\`
- Data: `VCM.BLUEPOS.Data\Promotion\`
- EF: `VCM.BLUEPOS.Data\EF\Central\` (Entities)
- View: `VCM.BLUEPOS\Views\SetupPromotion\`
- View: `VCM.BLUEPOS\Views\Promotion\`
- Controller: `VCM.BLUEPOS\Controllers\`
- JavaScript: `VCM.BLUEPOS\Content\setuppromotion.js`

### File Config
- DI Config: `VCM.BLUEPOS\Models\AutofacConfig.cs`
- Routing: `VCM.BLUEPOS\App_Start\RouteConfig.cs`

---

**END OF DOCUMENT**
