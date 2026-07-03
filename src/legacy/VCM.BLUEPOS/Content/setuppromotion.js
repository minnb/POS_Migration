


function GetItem(offerNo, lineNo, itemNo, offerType, conditionBuy, groupCode, isTotalBill, isSetupGet) {
  //  debugger
    var url = "SetupPromotion/GetItem";
    loading.showloading();
    var fromData = new FormData();
    fromData.append('itemNo', itemNo);
    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            if (result.Status == 200) {
                var data = result.Data;
                $(".itemBuy_" + offerNo + "_" + lineNo).val(data.No);
                $(".itemHiddenBuy_" + offerNo + "_" + lineNo).val(data.No);
                $(".descBuy_" + offerNo + "_" + lineNo).html(data.Description);
                $(".uomBuy_" + offerNo + "_" + lineNo).html(data.UOM);
                $(".txtQuantity_" + offerNo + "_" + lineNo).focus();
                ParamOfferBuy(offerNo, lineNo, offerType, conditionBuy, groupCode, isTotalBill, isSetupGet);
            }
            else {
                $(".itemBuy_" + offerNo + "_" + lineNo).val('');
                $(".itemHiddenBuy_" + offerNo + "_" + lineNo).val('');
                $(".descBuy_" + offerNo + "_" + lineNo).html('');
                $(".uomBuy_" + offerNo + "_" + lineNo).html('');
                toastr.error(result.Message);
            }
            loading.hideloading();
        },
        error: function () {
            loading.hideloading();
        }
    });
}
function ParamOfferBuy(offerNo, lineNo, offerType, conditionBuy, groupCode, isTotalBill, isSetupGet) {
   // debugger
    var lineType = $(".selectLineType_" + offerNo + "_" + lineNo).val();
    var itemNo = $(".itemBuy_" + offerNo + "_" + lineNo).val();
    var itemHidden = $(".itemHiddenBuy_" + offerNo + "_" + lineNo).val();
    var groupCodeHidden = $(".itemHiddenBuy_" + offerNo + "_" + lineNo).attr("data-groupcode");
    var lineTypeHidden = $(".hiddenLineType_" + offerNo + "_" + lineNo).val();
    var description = $(".descBuy_" + offerNo + "_" + lineNo).html();
    var uom = $(".uomBuy_" + offerNo + "_" + lineNo).html();
    // var discountType = $(".selectDiscountType_" + offerNo + "_" + lineNo).val();
    // var discountValue = $(".txtDiscountValue_" + offerNo + "_" + lineNo).val().replace(/\,/g, '');

    var quantity = $(".txtQuantity_" + offerNo + "_" + lineNo).val().replace(/\,/g, '');
    var scale = $(".selectScaleType_" + offerNo + "_" + lineNo).val();


    if (lineType == 0) {
        //InsertUpdateOfferBuy(offerNo, offerType, conditionBuy, lineNo, lineType, lineTypeHidden, itemNo, itemHidden, description, uom, quantity, scale, isTotalBill, isSetupGet);
    }
    else {
        var isChangeGroupCode = false;
        if (groupCode != "" && groupCode != groupCodeHidden) {
            isChangeGroupCode = true;
        }
        //InsertUpdateGroupItemOfferBuy(offerNo, offerType, conditionBuy, lineNo, lineType, groupCode, groupCode, quantity, scale, isTotalBill, isSetupGet);
    }
}

function ParamOfferGet(offerNo, offerType, conditionGet, lineNo, groupCode, isTotalBill, isSetupGet, totalBill) {

    var lineType = $(".selectLineTypeGet_" + offerNo + "_" + lineNo).val();
    var itemNo = $(".itemGet_" + offerNo + "_" + lineNo).val();
    var itemHidden = $(".itemHiddenGet_" + offerNo + "_" + lineNo).val();
    var lineTypeHidden = $(".hiddenLineTypeGet_" + offerNo + "_" + lineNo).val();
    var description = $(".descGet_" + offerNo + "_" + lineNo).html();
    var uom = $(".uomGet_" + offerNo + "_" + lineNo).html();
    var discountType = $(".selectDiscountTypeGet_" + offerNo + "_" + lineNo).val();
    var discountValue = $(".txtDiscountValueGet_" + offerNo + "_" + lineNo).val().replace(/\,/g, '');
    var quantity = $(".txtQuantityGet_" + offerNo + "_" + lineNo).val().replace(/\,/g, '');
    //var quantity = $(".txtQuantityGet_" + offerNo + "_" + lineNo).val();
    var scale = $(".selectScaleTypeGet_" + offerNo + "_" + lineNo).val();

    if (discountType == 0) {
        if (parseInt(discountValue) > 100) {
            toastr.error("Phần trăm giảm vui lòng không nhập lớn hơn 100");
            $(".txtDiscountValueGet_" + offerNo + "_" + lineNo).focus();
            return;
        }
    }
    if (lineType == 0) {
        InsertUpdateOfferGet(offerNo, offerType, conditionGet, lineNo, lineType, lineTypeHidden, itemNo, itemHidden, description, uom, discountType, discountValue, quantity, scale, isTotalBill, isSetupGet, totalBill);
    }
    else {
        InsertUpdateGroupItemOfferGet(offerNo, offerType, conditionGet, lineNo, lineType, groupCode, groupCode, discountType, discountValue, quantity, scale, isTotalBill, isSetupGet, totalBill);
    }
}

function GetOfferBuy(offerNo, isSetupBuy, offerType, isTotalBill, isSetupGet, buyLinkCat, checkMinValue, totalMinValue) {


   
    ///////////////////////////////////////////////
    var url = "SetupPromotion/_DetailOfferBuy";
    var fromData = new FormData();
    fromData.append('offerNo', offerNo);
    fromData.append('isSetupBuy', isSetupBuy);
    fromData.append('offerType', offerType);
    fromData.append('isTotalBill', isTotalBill);
    fromData.append('isSetupGet', isSetupGet);
    fromData.append('buyLinkCat', buyLinkCat);
    fromData.append('checkMinValue', checkMinValue);
    fromData.append('totalMinValue', totalMinValue);

    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            //debugger
            if (result.Status == 200) {
                var viewHtml = result.ViewPartial;
                $("#loadDataOfferBuy").html(viewHtml);
            }
            else {
                toastr.error(result.Message);
            }

            // loading.hideloading();
        },
        error: function () {
            //loading.hideloading();
        }
    });
}
function GetOfferGet(offerNo, offerType, isTotalBill, isSetupGet) {
    var url = "SetupPromotion/_DetailOfferGet";
    loading.showloading();
    var fromData = new FormData();
    fromData.append('offerNo', offerNo);
    fromData.append('offerType', offerType);
    fromData.append('isTotalBill', isTotalBill);
    fromData.append('isSetupGet', isSetupGet);

    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            //debugger
            if (result.Status == 200) {
                var viewHtml = result.ViewPartial;

                $("#loadDataOfferGet").html(viewHtml);
            }
            else {
                toastr.error(result.Message);
            }

            loading.hideloading();
        },
        error: function () {
            loading.hideloading();
        }
    });
}

function GetOfferHeader(bonusBuy) {
    loading.showloading();
    var url = "SetupPromotion/_DetailOfferHeader";
    var fromData = new FormData();
    fromData.append('BonusBuy', bonusBuy);
    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            //debugger
            if (result.Status == 200) {
                var viewHtml = result.ViewPartial;
                var offerHearder = result.Data.GetOfferHearder;
                $("#slOfferType").select2('val', [offerHearder.OfferType]);
                $("#slSalesType").select2('val', [offerHearder.SalesType || 'ALL']);
                $("#slStatus").select2('val', [offerHearder.Status]);
                $("#txtDescription").val(offerHearder.Description);
               // debugger;
                $('#fromDate').datepicker("setDate", moment(offerHearder.StartingDate).format("DD/MM/YYYY"));
                if (bonusBuy) {
                    $('#toDate').datepicker("setDate", moment(offerHearder.EndingDate).format("DD/MM/YYYY"));
                }
               
                var isVoucherHeader = offerHearder.IsVoucherHeader;
                if (isVoucherHeader == true) {
                    $("#chkVoucher").prop('checked', true);
                }
                else {
                    $("#chkVoucher").prop('checked', false);
                }

                var isApprove = offerHearder.IsApprove;
                if (isApprove == true) {
                    $(".titleAppove").html("Bỏ duyệt CTKM");
                    $("#hdIsApprove").val('false');
                }
                else {
                    $(".titleAppove").html("Duyệt CTKM");
                    $("#hdIsApprove").val('true');
                }


                $("#hdBuyLinkCat").val(offerHearder.BuyLinkCat);
                $("#hdGetLinkCat").val(offerHearder.GetLinkCat);
                $("#hdCheckMinValue").val(offerHearder.CheckMinValue);
                $("#hdTotalMinValue").val(offerHearder.TotalMinValue);

                // Đồng bộ các trường Advance từ DB vào sessionStorage để giữ nguyên khi lưu
                var _advRaw = sessionStorage.getItem("OfferAdvance");
                var _adv = _advRaw ? JSON.parse(_advRaw) : {};
                _adv.FromTime        = offerHearder.FromTime        || '';
                _adv.ToTime          = offerHearder.ToTime          || '';
                _adv.Mon             = offerHearder.Mon             || 0;
                _adv.Tue             = offerHearder.Tue             || 0;
                _adv.Wed             = offerHearder.Wed             || 0;
                _adv.Thu             = offerHearder.Thu             || 0;
                _adv.Fri             = offerHearder.Fri             || 0;
                _adv.Sat             = offerHearder.Sat             || 0;
                _adv.Sun             = offerHearder.Sun             || 0;
                _adv.LimitQty        = offerHearder.LimitQty        || 0;
                _adv.MemberOnly      = offerHearder.MemberOnly      || 0;
                _adv.PriorityBBY     = offerHearder.PriorityBBY     || 1;
                _adv.VoucherValidDay    = offerHearder.VoucherValidDay    || 0;
                _adv.VoucherLimitNumber = offerHearder.VoucherLimitNumber || 0;
                _adv.MemberCode      = offerHearder.MemberCode      || '';
                _adv.NumOfDays       = offerHearder.NumOfDays       || 0;
                _adv.VoucherFromDateStr = offerHearder.VoucherFromDate
                    ? moment(offerHearder.VoucherFromDate).format("DD/MM/YYYY") : '';
                _adv.VoucherToDateStr = offerHearder.VoucherToDate
                    ? moment(offerHearder.VoucherToDate).format("DD/MM/YYYY") : '';
                sessionStorage.setItem("OfferAdvance", JSON.stringify(_adv));

                $("#loadDataOfferHeader").html(viewHtml).attr("data-loaded-ma", bonusBuy || "");

            }
            else {
                ClearTable();
                $("#loadDataOfferHeader").attr("data-loaded-ma", "");
                toastr.error(result.Message);
            }

            loading.hideloading();
        },
        error: function () {
            loading.hideloading();
        }
    });
}


function InsertUpdateOfferBuy(offerNo, offerType, conditionBuy, lineNo, lineType, lineTypeOld, itemNo, itemHidden, description, uom, quantity, scale, isTotalBill, isSetupGet) {
    var url = "SetupPromotion/InsertUpdateOfferBuy";
    var fromData = new FormData();
    fromData.append('OfferNo', offerNo);
    fromData.append('LineNo', lineNo);
    fromData.append('LineType', lineType);
    fromData.append('No', itemNo);
    fromData.append('Description', description);
    fromData.append('UnitOfMeasure', uom);
    //fromData.append('DiscountType', discountType);
    //fromData.append('DiscountValue', discountValue);
    fromData.append('Quantity', quantity);
    fromData.append('ScaleType', scale);
    fromData.append('BonusBuyNo', "");

    fromData.append('isTotalBill', isTotalBill);
    fromData.append('isSetupGet', isSetupGet);
    fromData.append('conditionBuy', conditionBuy);
    fromData.append('offerType', offerType);
    fromData.append('itemHidden', itemHidden);

    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            //debugger
            if (result.Status == 200) {
                if (lineType == 0) {
                    $(".itemHiddenBuy_" + offerNo + "_" + lineNo).val(itemNo);
                    $(".hiddenLineType_" + offerNo + "_" + lineNo).val(lineType);


                    $(".itemBuy_" + offerNo + "_" + lineNo).val(itemNo);
                    $(".descBuy_" + offerNo + "_" + lineNo).html(description);
                    $(".uomBuy_" + offerNo + "_" + lineNo).html(uom);

                    var html = "";
                    html += "<a onclick=\"DeleteOfferBuy('" + offerNo + "','" + lineNo + "','" + itemNo + "','" + lineType + "','')\" title=\"Xóa\" class=\"btn btn-icon btn-light-danger\" data-offerno='" + offerNo + "' data-lineno='" + lineNo + "' data-no='" + itemNo + "'><i class=\"la la-close\"></i><span class=\"pulse-ring\"></span></a>";
                    $(".spanAction_" + offerNo + "_" + lineNo).html(html);



                    if (itemNo != "") {
                        $(".selectLineType_" + offerNo + "_" + lineNo).attr("disabled", true);
                    }
                    if (lineType != lineTypeOld) {
                        $("#offBuy-tab").trigger("click");
                    }
                    $('#modalListItemSetup').modal('hide');
                    $('.modal-backdrop').remove();

                }
                else {
                }
                toastr.success(result.Message);
            }
            else {
                var dataHidden = result.Data;
                if (dataHidden != null) {
                    $(".itemBuy_" + offerNo + "_" + lineNo).val(dataHidden.No);
                    $(".descBuy_" + offerNo + "_" + lineNo).html(dataHidden.Description);
                    $(".uomBuy_" + offerNo + "_" + lineNo).html(dataHidden.UnitOfMeasure);
                }
                toastr.error(result.Message);

            }

            loading.hideloading();
        },
        error: function () {
            loading.hideloading();
        }
    });
}

function InsertUpdateGroupItemOfferBuy(offerNo, offerType, conditionBuy, lineNo, lineType, listGroupCode, groupCode, quantity, scale, isTotalBill, isSetupGet) {
    //debugger
    var url = "SetupPromotion/InsertUpdateOfferBuy";
    var fromData = new FormData();
    fromData.append('OfferNo', offerNo);
    fromData.append('LineNo', lineNo);
    fromData.append('LineType', lineType);
    fromData.append('GroupCode', listGroupCode);
    fromData.append('Quantity', quantity);
    fromData.append('ScaleType', scale);
    fromData.append('BonusBuyNo', groupCode);

    fromData.append('isTotalBill', isTotalBill);
    fromData.append('isSetupGet', isSetupGet);
    fromData.append('conditionBuy', conditionBuy);
    fromData.append('offerType', offerType);
    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            //debugger
            if (result.Status == 200) {
                if (listGroupCode == "" || listGroupCode != groupCode) {
                    $('#modalListItemSetup').modal('hide');
                    $('.modal-backdrop').remove();
                    $("#offBuy-tab").trigger("click");
                }

                toastr.success(result.Message);
            }
            else {
                //var dataHidden = result.Data;
                //if (dataHidden != null) {
                //    $(".itemBuy_" + offerNo + "_" + lineNo).val(dataHidden.No);
                //    $(".descBuy_" + offerNo + "_" + lineNo).html(dataHidden.Description);
                //    $(".uomBuy_" + offerNo + "_" + lineNo).html(dataHidden.UnitOfMeasure);
                //}
                toastr.error(result.Message);

            }

            //loading.hideloading();
        },
        error: function () {
            //loading.hideloading();
        }
    });
}

function InsertUpdateGroupItemOfferGet(offerNo, offerType, conditionGet, lineNo, lineType, listGroupCode, groupCode, discountType, discountValue, quantity, scale, isTotalBill, isSetupGet, totalBill) {

    var url = "SetupPromotion/InsertUpdateOfferGet";
    var fromData = new FormData();
    fromData.append('OfferNo', offerNo);
    fromData.append('LineNo', lineNo);
    fromData.append('LineType', lineType);
    fromData.append('GroupCode', listGroupCode);
    // fromData.append('No', itemNo);
    //fromData.append('Description', description);
    //fromData.append('UnitOfMeasure', uom);
    fromData.append('DiscountType', discountType);
    fromData.append('DiscountValue', discountValue);
    fromData.append('Quantity', quantity);
    fromData.append('ScaleType', scale);
    fromData.append('BonusBuyNo', groupCode);
    fromData.append('Step', totalBill);


    fromData.append('isTotalBill', isTotalBill);
    fromData.append('isSetupGet', isSetupGet);
    fromData.append('conditionGet', conditionGet);
    fromData.append('offerType', offerType);

    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            if (result.Status == 200) {
                if (listGroupCode == "" || listGroupCode != groupCode) {
                    $('#modalListItemSetupGet').modal('hide');
                    $('.modal-backdrop').remove();
                    $("#offGet-tab").trigger("click");
                }

                toastr.success(result.Message);
            }
            else {

                toastr.error(result.Message);

            }

            //loading.hideloading();
        },
        error: function () {
            //loading.hideloading();
        }
    });
}

function InsertUpdateOfferGet(offerNo, offerType, conditionGet, lineNo, lineType, lineTypeOld, itemNo, itemHidden, description, uom, discountType, discountValue, quantity, scale, isTotalBill, isSetupGet, totalBill) {

    var url = "SetupPromotion/InsertUpdateOfferGet";
    var fromData = new FormData();
    fromData.append('OfferNo', offerNo);
    fromData.append('LineNo', lineNo);
    fromData.append('LineType', lineType);
    fromData.append('No', itemNo);
    fromData.append('Description', description);
    fromData.append('UnitOfMeasure', uom);
    fromData.append('DiscountType', discountType);
    fromData.append('DiscountValue', discountValue);
    fromData.append('Quantity', quantity);
    fromData.append('ScaleType', scale);
    fromData.append('BonusBuyNo', "");
    fromData.append('Step', totalBill);


    fromData.append('isTotalBill', isTotalBill);
    fromData.append('isSetupGet', isSetupGet);
    fromData.append('conditionGet', conditionGet);
    fromData.append('offerType', offerType);
    fromData.append('itemHidden', itemHidden);

    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            debugger
            if (result.Status == 200) {
                $(".itemHiddenGet_" + offerNo + "_" + lineNo).val(itemNo);
                if (itemNo != itemHidden) {
                    var html = "";
                    html += "<a onclick=\"DeleteOfferGet('" + offerNo + "','" + lineNo + "','" + itemNo + "'," + isTotalBill + "," + isSetupGet + ")\" title=\"Xóa\" class=\"btn btn-icon btn-light-danger\" data-offerno='" + offerNo + "' data-lineno='" + lineNo + "' data-no='" + itemNo + "'><i class=\"la la-close\"></i><span class=\"pulse-ring\"></span></a>";

                    $(".spanActionGet_" + offerNo + "_" + lineNo).html(html);
                }

                $(".itemGet_" + offerNo + "_" + lineNo).val(itemNo);
                $(".descGet_" + offerNo + "_" + lineNo).html(description);
                $(".uomGet_" + offerNo + "_" + lineNo).html(uom);


                $('#modalListItemSetupGet').modal('hide');
                $('.modal-backdrop').remove();

                if (lineType != lineTypeOld) {
                    $("#offGet-tab").trigger("click");

                }

                toastr.success(result.Message);
            }
            else {
                var dataHidden = result.Data;
                if (dataHidden != null) {
                    $(".itemGet_" + offerNo + "_" + lineNo).val(dataHidden.No);
                    $(".descGet_" + offerNo + "_" + lineNo).html(dataHidden.Description);
                    $(".uomGet_" + offerNo + "_" + lineNo).html(dataHidden.UnitOfMeasure);
                }
                toastr.error(result.Message);

            }

            loading.hideloading();
        },
        error: function () {
            loading.hideloading();
        }
    });
}



function GetItemTabGet(offerNo, lineNo, itemNo, offerType, conditionGet, groupCode, isTotalBill, isSetupGet, totalBill) {
    //debugger
    var url = "SetupPromotion/GetItem";
    loading.showloading();
    var fromData = new FormData();
    fromData.append('itemNo', itemNo);
    $.ajax({
        type: "POST",
        url: url,
        data: fromData,
        contentType: "application/json",
        dataType: "json",
        contentType: false,
        processData: false,
        success: function (result) {
            if (result.Status == 200) {
                var data = result.Data;
                $(".itemGet_" + offerNo + "_" + lineNo).val(data.No);
                $(".itemHiddenGet_" + offerNo + "_" + lineNo).val(data.No);
                $(".descGet_" + offerNo + "_" + lineNo).html(data.Description);
                $(".uomGet_" + offerNo + "_" + lineNo).html(data.UOM);
                $(".txtQuantityGet_" + offerNo + "_" + lineNo).focus();
                ParamOfferGet(offerNo, offerType, conditionGet, lineNo, groupCode, isTotalBill, isSetupGet, totalBill);
            }
            else {
                $(".itemGet_" + offerNo + "_" + lineNo).val('');
                $(".itemHiddenGet_" + offerNo + "_" + lineNo).val('');
                $(".descGet_" + offerNo + "_" + lineNo).html('');
                $(".uomGet_" + offerNo + "_" + lineNo).html('');
                toastr.error(result.Message);
            }
            loading.hideloading();
        },
        error: function () {
            loading.hideloading();
        }
    });
}