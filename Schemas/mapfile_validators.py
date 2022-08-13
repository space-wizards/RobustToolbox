from yamale.validators import Validator
import yaml

class Component(Validator):
    tag = "comp"

    def _is_valid(self, value):
        data = yaml.safe_load(value)
        if data["type"]:
            return True
        return False