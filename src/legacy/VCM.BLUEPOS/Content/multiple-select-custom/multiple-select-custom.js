class MultiselectLibrary {
    constructor (block, url) {
        this.block = block;
        this.url = url;

        if (!$(block).attr('id')) {
            var uniqueId = 'auto-id-' + Date.now();
            $(block).attr('id', uniqueId);
        }

        this.selectId = "store-list-" + $(block).attr('id');
        this.pageIndex = 1;
        this.currentSelected = 0;
        this.searchText = "";
        this.searchTextAll = "";
        this.tempTotal = 0;
        this.total = 0;
        this.unSelectedList = [];
        this.isNextPage = true;
        this.isCheckAll = false;
        this.setSelectedOption();
        this.setupSearch();
        this.setupLoadMore();
        this.updatePageindex();
        this.updateNextPage();
        this.getAndInitializeStoreList();
    }

    loadingLoader() {
        var self = this;
        var sourceHeight = $(self.block + ' .multiselect-container').height();
        $('#loadingSearch').height(sourceHeight);
        $('#loadingSearch').show();
    }

    hidingLoader () {
        $('#loadingSearch').hide();
    }

    updatePageindex (number) {
        this.pageIndex = number;
    }

    updateNextPage (next) {
        this.isNextPage = next;
    }

    updateCheckAllStatus (checked) {
        this.isCheckAll = checked;
    }

    updateTempTotal (total) {
        this.tempTotal = total;
    }

    updateTotal (total) {
        this.total = total;
    }

    addUnSelectedList (option) {
        this.unSelectedList.push(option);
    }

    deleteUnSelectedList (option) {
        var indexToDelete = this.unSelectedList.indexOf(option);
        // If the item is found, remove it using splice
        if (indexToDelete !== -1) {
            this.unSelectedList.splice(indexToDelete, 1);
        }
    }

    updateCurrentSelected (selected) {
        this.currentSelected = selected;
    }

    getTotal() {
        return this.total;
    }

    getCurrentSelected() {
        return this.currentSelected;
    }

    getIsCheckAll() {
        return this.isCheckAll;
    }

    getUnSelectedList () {
        return this.unSelectedList;
    }

    getSelectedList() {
        if (this.unSelectedList.length > 0) {
            return $("#" + this.selectId).val().filter(v => v !== ""); // loại bỏ option "All" nếu có
        }
        else {
            if (this.isCheckAll) return [""];
            return $("#" + this.selectId).val();
        }
    }

    getSearchText () {
        return this.searchText;
    }

    getSearchTextAll () {
        return this.searchTextAll;
    }

    rebuildMultiselect (response) {
        var self = this;
        $("#"+self.selectId).empty();
        $("#" +self.selectId).append("<option value='0' class='all-check'> Chọn tất cả</option>");
        //$(self.selectId).append("<option value='ALL'>All - Tất cả cửa hàng </option>");
        $.each(response.ComboxOptions, function (i, option) {
            $("#" +self.selectId).append("<option value='" + option.Value + "'>" + option.Value + " - " + option.Text + " </option>");
        });
        $("#" +self.selectId).multiselect('rebuild');
        if (self.isCheckAll) {
            self.setSelectedOption(response.ComboxOptions, response.UnSelectedOptions || []);
        } else {
            self.setSelectedOption(response.SelectedOptions, response.UnSelectedOptions || []);
        }

        $(self.block + ' .search-multiselect-custom').off('keyup');
        $(self.block + ' .multiselect-container').off('scroll');
        self.setupSearch();
        self.setupLoadMore();
    }

    setSelectedOption (data, unseleted) {
        var self = this;
        if (data != null && data != 'undefined') {
            $.each(data, function (i, option) {
                if (self.isCheckAll) {
                    if (!unseleted.includes(option.Value)) {
                        $("#" +self.selectId + " option[value='" + option.Value + "']").attr("selected", 1);
                    }
                } else {
                    $("#" +self.selectId + " option[value='" + option.Value + "']").attr("selected", 1);
                }
            });
            $("#" +self.selectId).multiselect("rebuild");
        }
    }

    fetchMoreItems (keyword) {
        var self = this;
        $.ajax({
            type: "POST",
            url: self.url,
            global: false,
            data: {
                SearchText: self.searchText,
                PageIndex: self.pageIndex,
                PageSize: 50,
                IsCheckAll: self.isCheckAll,
                IsLoadMore: true,
                SeletedList: $("#" +self.selectId).val(),
                UnSeletedList: self.unSelectedList
            },
            dataType: "json",
            success: function (response) {
                self.updatePageindex(response.PageIndex + 1);
                self.updateNextPage(response.IsNextPage);
                self.updateTempTotal(response.Total);

                if (!self.IsCheckAll) {
                    self.updateTotal(response.Total);
                }

                if (self.IsCheckAll) {
                    var unSelectedOptions = response.UnSelectedOptions || [];
                    var unSelectedLength = unSelectedOptions.length;
                    self.updateCurrentSelected(self.total - unSelectedLength);
                }
                if (response.ComboxOptions != null && response.ComboxOptions.length > 0) {
                    $.each(response.ComboxOptions, function (i, option) {
                        $("#" +self.selectId).append("<option value='" + option.Value + "'>" + option.Value + " - " + option.Text + " </option>");
                    });
                }
                else {
                    if (response.Message != null && response.Message != undefined && response.Message != '') {
                        toastr.warning(response.Message);
                    }
                }

                $("#" +self.selectId).multiselect('rebuild');
                self.setSelectedOption(response.SelectedOptions, response.UnSelectedOptions || []);
                $(self.block + ' .search-multiselect-custom').off('keyup');
                self.hidingLoader();
                self.setupSearch();
            },
            error: function () {
                console.error('Error fetching more items.');
            }
        });
    }

    setupSearch () {
        var self = this;
        $(self.block + ' .search-multiselect-custom').on("keyup", self.delay(function (e) {
            const key = e.key;
            if ((key >= 'a' && key <= 'z') || // A-Z
                (key >= '0' && key <= '9') || // 0-9
                key === 'Backspace' ||
                key === 'Enter' ||
                key === 'Shift' ||
                key === 'Control' ||
                (key === 'Control' && key === 'v') ||
                (key >= 'numpad0' && key <= 'numpad9')) { // Numpad 0-9
                self.searchText = e.target.value;
                self.loadingLoader();
                self.addEventSearching(self.searchText);
            }
        }, 800));

        $(self.block + ' .search-multiselect-custom').on('focus', function () {
            $(self.block + ' .multiselect-container').addClass('show');
        });

        $(document).mouseup(function (e) {
            var targetElement = $(self.block);
            if (!targetElement.is(e.target) && targetElement.has(e.target).length === 0) {
                $(self.block + ' .multiselect-container').removeClass('show');
                $('.search-multiselect-custom').hide();
            }
        });
    }

    addEventSearching(e) {
        var keyword = e;
        var self = this;
        self.updatePageindex(1);
        $.ajax({
            type: "POST",
            url: self.url,
            global: false,
            data: {
                SearchText: keyword,
                PageIndex: self.pageIndex,
                IsCheckAll: self.isCheckAll,
                PageSize: 50,
                IsLoadMore: false,
                SeletedList: $("#" +self.selectId).val(),
                UnSeletedList: self.unSelectedList
            },
            dataType: "json",
            success: function (response) {
                self.rebuildMultiselect(response);
                self.updatePageindex(response.PageIndex + 1);
                self.updateNextPage(response.IsNextPage);
                self.updateTempTotal(response.Total);
                if (!self.isCheckAll) {
                    self.updateTotal(response.Total);
                }
                if (self.isCheckAll) {
                    var unSelectedOptions = response.UnSelectedOptions || [];
                    var unSelectedLength = unSelectedOptions.length;
                    self.updateCurrentSelected(self.total - unSelectedLength);
                }
                self.hidingLoader();

                $(self.block + ' .multiselect-container').off('scroll');
                self.setupLoadMore();

                if (response.Message != null && response.Message != undefined && response.Message != '') {
                    toastr.warning(response.Message);
                }
            }
        });
    }

    setupLoadMore() {
        var self = this;
        $(self.block + ' .multiselect-container').on('scroll', function () {
            if ($(this).scrollTop() + $(this).innerHeight() >= $(this)[0].scrollHeight - 2 && self.isNextPage == true) {
                self.loadingLoader();
                self.fetchMoreItems();
            }
        });
    }

    delay (callback, ms) {
        var timer = 0;
        return function () {
            var context = this, args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () {
                callback.apply(context, args);
            }, ms || 0);
        };
    }

    getAndInitializeStoreList() {
        var self = this;
        var html = '<input placeholder="Tìm kiếm cửa hàng" class="search-multiselect-custom" name="SearchTextStore"></input>';
        html += '<select class="form-control custom-selectall-text" multiple="multiple" style="width: 100%;" id="' + self.selectId + '" name="ListStoreNo">';

        $.ajax({
            type: "POST",
            url: self.url,
            global:false,
            data: {
                SearchText: "",
                PageIndex: 1,
                PageSize: 50,
                IsLoadMore: false,
                IsCheckAll: self.isCheckAll
            },
            dataType: "json",
            success: function (response) {
                self.updatePageindex(response.PageIndex + 1);
                self.updateNextPage(response.IsNextPage);
                self.updateTempTotal(response.Total);
                self.updateTotal(response.Total);

                if (response.ComboxOptions != null && response.ComboxOptions.length > 0) {
                    //html += "<option value='ALL'>All - Tất cả cửa hàng </option>"
                    response.ComboxOptions.forEach((name, i) => {
                        html += "<option value='" + name.Value + "'>" + name.Value + " - " + name.Text + " </option>"
                    });
                }
                else {
                    if (response.Message != null && response.Message != undefined && response.Message != '') {
                        toastr.warning(response.Message);
                    }
                }

                html += '</select>';
                html += '<div id="loadingSearch" class="loadingSearch">';
                //html += '<img src="/assets/images/loader.gif" class="img-custom-loader"/>';
                html += '</div>';

                $(self.block).html(html);
                $("#" +self.selectId).multiselect({
                    nSelectedText: 'Cửa hàng đã chọn',
                    nonSelectedText: 'Chọn ST/CH',
                    selectAllText: 'Chọn tất cả',
                    allSelectedText: 'Chọn tất cả',
                    includeSelectAllOption: true,
                    selectAllValue: 0,
                    enableFiltering: false,
                    includeFilterClearBtn: false,
                    enableCaseInsensitiveFiltering: false,
                    onChange: function (option, checked, select) {
                        if (self.isCheckAll && checked) {
                            self.updateCurrentSelected(self.currentSelected + 1);
                            self.deleteUnSelectedList(option[0].attributes[0].value);
                        } else if (self.isCheckAll) {
                            self.updateCurrentSelected(self.currentSelected - 1);
                            self.addUnSelectedList(option[0].attributes[0].value);
                        }

                        //sontp 21/05/2024
                        if (checked && $('.search-multiselect-custom').val().trim() !== '') {
                            $('.search-multiselect-custom').val("");
                            self.searchText = "";
                            self.loadingLoader();
                            self.addEventSearching("");
                            $('.search-multiselect-custom').focus();
                        }
                        //
                    },
                    onSelectAll: function () {
                        self.updateCurrentSelected(self.getTotal());
                        self.searchTextAll = self.searchText;
                        self.updateTotal(self.tempTotal);
                        self.updateCheckAllStatus(true);
                        self.unSelectedList = [];
                    },
                    onDeselectAll: function () {
                        $("#" +self.selectId).val([])
                        self.updateCurrentSelected(0);
                        self.updateCheckAllStatus(false);
                        self.unSelectedList = [];
                    },
                    onDropdownShown: function (event) {
                        event.preventDefault();
                        $('.search-multiselect-custom').val("");
                        $('.search-multiselect-custom').show();
                        $('.search-multiselect-custom').focus();
                        //$('.search-multiselect-custom').trigger($.Event('keyup', { key: 'Enter' }));
                    },
                    onDropdownHide: function (event) {
                        event.preventDefault();
                    },
                    maxHeight: 300
                });

                self.setupSearch();
                self.setupLoadMore();
            },
            error: function () {
                toastr.error('Lỗi hệ thống');
            }

        });
    }
};